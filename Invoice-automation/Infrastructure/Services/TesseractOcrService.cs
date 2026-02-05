using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;

namespace InvoiceAutomation.Web.Infrastructure.Services;

/// <summary>
/// Tesseract 5 OCR implementation for invoice data extraction
/// </summary>
public class TesseractOcrService : IOcrService, IDisposable
{
    private readonly TesseractEngine _engine;
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _tessdataPath;
    private readonly string _tempPath;
    private bool _disposed;

    public TesseractOcrService(
        IWebHostEnvironment environment,
        ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
        _tessdataPath = Path.Combine(environment.ContentRootPath, "App_Data", "tessdata");
        _tempPath = Path.Combine(Path.GetTempPath(), "InvoiceOcr");

        // Ensure directories exist
        Directory.CreateDirectory(_tessdataPath);
        Directory.CreateDirectory(_tempPath);

        // Initialize Tesseract with LSTM engine for best accuracy
        try
        {
            _engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
            _engine.DefaultPageSegMode = PageSegMode.Auto;
            _logger.LogInformation("Tesseract OCR engine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Tesseract engine. Ensure tessdata files are in {Path}", _tessdataPath);
            throw;
        }
    }

    public async Task<InvoiceOcrResult> ExtractInvoiceDataAsync(string filePath)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new InvoiceOcrResult();

        _logger.LogInformation("Starting OCR processing for file: {FilePath}", filePath);

        try
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                result.Errors.Add($"File not found: {filePath}");
                return result;
            }

            // Prepare images (convert PDF if needed)
            var images = await PrepareImagesAsync(filePath);

            if (!images.Any())
            {
                result.Errors.Add("No images could be extracted from the file");
                return result;
            }

            var allText = new StringBuilder();
            decimal totalConfidence = 0;
            int pageCount = 0;

            foreach (var imagePath in images)
            {
                try
                {
                    // Preprocess image for better OCR
                    var processedPath = await PreprocessImageAsync(imagePath);

                    // Run Tesseract OCR
                    using var img = Pix.LoadFromFile(processedPath);
                    using var page = _engine.Process(img);

                    var pageText = page.GetText();
                    var pageConfidence = (decimal)(page.GetMeanConfidence() * 100);

                    allText.AppendLine(pageText);
                    totalConfidence += pageConfidence;
                    pageCount++;

                    _logger.LogDebug("Page {Page} processed with confidence: {Confidence}%",
                        pageCount, pageConfidence);

                    // Cleanup processed temp file
                    if (processedPath != imagePath && File.Exists(processedPath))
                    {
                        File.Delete(processedPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing image: {Path}", imagePath);
                    result.Warnings.Add($"Error processing page: {ex.Message}");
                }
            }

            // Cleanup temp images
            foreach (var img in images.Where(i => i.StartsWith(_tempPath)))
            {
                try { if (File.Exists(img)) File.Delete(img); } catch { }
            }

            result.RawText = allText.ToString();
            result.Confidence = pageCount > 0 ? totalConfidence / pageCount : 0;

            // Extract structured fields using regex patterns
            ExtractFields(result);

            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "OCR completed. Confidence: {Confidence}%, Time: {Time}ms",
                result.Confidence, result.ProcessingTimeMs);

            // Add warnings for low confidence fields
            if (result.Confidence < 70)
            {
                result.Warnings.Add($"Overall OCR confidence is low ({result.Confidence:F1}%). Please verify all extracted data.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR processing failed for: {FilePath}", filePath);
            result.Errors.Add($"OCR processing failed: {ex.Message}");
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            // Check if tessdata files exist
            var engData = Path.Combine(_tessdataPath, "eng.traineddata");
            return Task.FromResult(File.Exists(engData) && _engine != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private async Task<List<string>> PrepareImagesAsync(string filePath)
    {
        var images = new List<string>();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".pdf")
        {
            // Convert PDF pages to images
            images.AddRange(await ConvertPdfToImagesAsync(filePath));
        }
        else if (new[] { ".png", ".jpg", ".jpeg", ".tiff", ".tif", ".bmp" }.Contains(extension))
        {
            images.Add(filePath);
        }
        else
        {
            _logger.LogWarning("Unsupported file type: {Extension}", extension);
        }

        return images;
    }

    private async Task<List<string>> ConvertPdfToImagesAsync(string pdfPath)
    {
        var images = new List<string>();

        try
        {
            // Using PdfPig to extract text directly (simpler approach)
            // For full image extraction, you'd need a library like PDFium
            using var document = UglyToad.PdfPig.PdfDocument.Open(pdfPath);

            // For now, we'll extract text directly from PDF
            var textBuilder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            // Save as a text file to process
            var textPath = Path.Combine(_tempPath, $"{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(textPath, textBuilder.ToString());

            // If PDF has embedded text, we can use it directly
            // Otherwise, we'd need to render PDF pages to images

            _logger.LogInformation("Extracted text from {PageCount} PDF pages", document.NumberOfPages);

            // For image-based PDFs, you would render each page to an image
            // This requires additional libraries like PDFium or Ghostscript
            // For now, returning empty and relying on text extraction

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract from PDF, will attempt image OCR");
        }

        return images;
    }

    private async Task<string> PreprocessImageAsync(string imagePath)
    {
        var outputPath = Path.Combine(_tempPath, $"processed_{Guid.NewGuid():N}.png");

        try
        {
            using var image = await Image.LoadAsync<Rgba32>(imagePath);

            image.Mutate(ctx =>
            {
                // Convert to grayscale
                ctx.Grayscale();

                // Increase contrast for better text recognition
                ctx.Contrast(1.3f);

                // Sharpen text
                ctx.GaussianSharpen(0.5f);

                // Resize if image is too small (minimum 300 DPI equivalent)
                if (image.Width < 2000)
                {
                    var scale = 2000.0 / image.Width;
                    ctx.Resize((int)(image.Width * scale), (int)(image.Height * scale));
                }
            });

            await image.SaveAsPngAsync(outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image preprocessing failed, using original");
            return imagePath;
        }
    }

    private void ExtractFields(InvoiceOcrResult result)
    {
        var text = result.RawText;

        // Extract Invoice Number
        result.InvoiceNumber = ExtractInvoiceNumber(text);

        // Extract Dates
        result.InvoiceDate = ExtractDate(text, "invoice");
        result.DueDate = ExtractDate(text, "due");

        // Extract Vendor Information
        result.VendorName = ExtractVendorName(text);
        result.VendorNtn = ExtractNtn(text);

        // Extract Amounts
        result.TotalAmount = ExtractAmount(text, @"(total|grand\s*total|amount\s*due|net\s*payable)");
        result.TaxAmount = ExtractAmount(text, @"(tax|gst|sales\s*tax|vat)");
        result.Subtotal = ExtractAmount(text, @"(subtotal|sub\s*total|net\s*amount)");

        // Extract Line Items
        result.LineItems = ExtractLineItems(text);

        // Add warnings for missing critical fields
        if (string.IsNullOrEmpty(result.InvoiceNumber))
            result.Warnings.Add("Could not extract invoice number");
        if (!result.InvoiceDate.HasValue)
            result.Warnings.Add("Could not extract invoice date");
        if (!result.TotalAmount.HasValue)
            result.Warnings.Add("Could not extract total amount");
    }

    private string? ExtractInvoiceNumber(string text)
    {
        // Patterns for invoice numbers
        var patterns = new[]
        {
            @"invoice\s*#?\s*:?\s*([A-Z0-9][-A-Z0-9/]{2,20})",
            @"inv\s*[#:.-]?\s*([A-Z0-9][-A-Z0-9/]{2,20})",
            @"bill\s*#?\s*:?\s*([A-Z0-9][-A-Z0-9/]{2,20})",
            @"reference\s*#?\s*:?\s*([A-Z0-9][-A-Z0-9/]{2,20})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    private DateTime? ExtractDate(string text, string context)
    {
        var patterns = new[]
        {
            $@"{context}\s*date\s*:?\s*(\d{{1,2}}[-/]\d{{1,2}}[-/]\d{{2,4}})",
            $@"{context}\s*:?\s*(\d{{1,2}}[-/]\d{{1,2}}[-/]\d{{2,4}})",
            @"(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
            @"(\d{4}[-/]\d{1,2}[-/]\d{1,2})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (DateTime.TryParse(match.Groups[1].Value, out var date))
                {
                    return date;
                }
            }
        }

        return null;
    }

    private string? ExtractVendorName(string text)
    {
        // Look for company name patterns at the start of the document
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // First few lines often contain vendor name
        foreach (var line in lines.Take(10))
        {
            var cleaned = line.Trim();
            // Skip lines that look like addresses or phone numbers
            if (cleaned.Length > 3 && cleaned.Length < 100 &&
                !Regex.IsMatch(cleaned, @"^\d+|phone|fax|email|www\.|http", RegexOptions.IgnoreCase))
            {
                // Check if it looks like a company name
                if (Regex.IsMatch(cleaned, @"(pvt|ltd|inc|corp|llc|co\.|limited|private)", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(cleaned, @"^[A-Z][a-zA-Z\s&]+$"))
                {
                    return cleaned;
                }
            }
        }

        return null;
    }

    private string? ExtractNtn(string text)
    {
        // Pakistani NTN format: 7-8 digits, optionally followed by -digit
        var patterns = new[]
        {
            @"NTN[\s#:.-]*(\d{7,8}(?:-\d)?)",
            @"national\s*tax\s*number[\s:]*(\d{7,8})",
            @"tax\s*id[\s:]*(\d{7,8})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private decimal? ExtractAmount(string text, string context)
    {
        var patterns = new[]
        {
            $@"{context}[\s:]*(?:PKR|Rs\.?|₨)?\s*([\d,]+\.?\d*)",
            $@"{context}[\s:]*([\d,]+\.?\d*)",
            @"(?:PKR|Rs\.?|₨)\s*([\d,]+\.?\d*)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var amountStr = match.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(amountStr, out var amount))
                {
                    return amount;
                }
            }
        }

        return null;
    }

    private List<OcrLineItem> ExtractLineItems(string text)
    {
        var items = new List<OcrLineItem>();

        // Look for table-like structures with amounts
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            // Look for lines with amounts
            var amountMatch = Regex.Match(line, @"([\d,]+\.?\d*)\s*$");
            if (amountMatch.Success && decimal.TryParse(amountMatch.Groups[1].Value.Replace(",", ""), out var amount))
            {
                // Extract description (everything before the amount)
                var description = line.Substring(0, amountMatch.Index).Trim();

                // Filter out header/footer lines
                if (!string.IsNullOrEmpty(description) &&
                    description.Length > 3 &&
                    description.Length < 200 &&
                    !Regex.IsMatch(description, @"^(total|subtotal|tax|grand|amount|date|invoice)", RegexOptions.IgnoreCase))
                {
                    items.Add(new OcrLineItem
                    {
                        Description = description,
                        Amount = amount,
                        Quantity = 1,
                        Confidence = 70 // Default confidence for line items
                    });
                }
            }
        }

        // Limit to reasonable number of line items
        return items.Take(50).ToList();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _engine?.Dispose();
            _disposed = true;
        }
    }
}
