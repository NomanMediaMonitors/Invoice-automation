using InvoiceAutomation.Web.Core.Interfaces;

namespace InvoiceAutomation.Web.Infrastructure.Services;

/// <summary>
/// Local file storage service for invoice files
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    private static readonly string[] AllowedExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".tiff", ".tif", ".bmp" };
    private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB

    // Magic bytes for file type validation
    private static readonly Dictionary<string, byte[]> FileSignatures = new()
    {
        { ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
        { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 } }, // PNG
        { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },       // JPEG
        { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },      // JPEG
        { ".tiff", new byte[] { 0x49, 0x49, 0x2A, 0x00 } }, // TIFF (little-endian)
        { ".tif", new byte[] { 0x49, 0x49, 0x2A, 0x00 } },  // TIFF
        { ".bmp", new byte[] { 0x42, 0x4D } }              // BMP
    };

    public LocalFileStorageService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;

        // Get upload path from configuration or use default
        _uploadPath = configuration["FileStorage:UploadPath"]
            ?? Path.Combine(environment.ContentRootPath, "Uploads");

        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(Path.Combine(_uploadPath, "Invoices"));

        _logger.LogInformation("File storage initialized at: {Path}", _uploadPath);
    }

    public async Task<string> SaveInvoiceFileAsync(Guid companyId, IFormFile file)
    {
        // Validate file
        ValidateFile(file);

        // Generate organized path: /Invoices/{CompanyId}/{Year}/{Month}/{guid}_{filename}
        var now = DateTime.UtcNow;
        var relativeFolderPath = Path.Combine(
            "Invoices",
            companyId.ToString(),
            now.Year.ToString(),
            now.Month.ToString("D2"));

        var physicalFolderPath = Path.Combine(_uploadPath, relativeFolderPath);
        Directory.CreateDirectory(physicalFolderPath);

        // Generate unique filename
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
        var uniqueFileName = $"{Guid.NewGuid():N}_{sanitizedFileName}{extension}";

        var relativeFilePath = Path.Combine(relativeFolderPath, uniqueFileName);
        var physicalFilePath = Path.Combine(_uploadPath, relativeFilePath);

        // Save file
        using (var stream = new FileStream(physicalFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Validate file content after saving
        await ValidateFileContentAsync(physicalFilePath, extension);

        _logger.LogInformation("File saved: {Path}, Size: {Size} bytes", relativeFilePath, file.Length);

        // Return relative path with forward slashes for URL compatibility
        return relativeFilePath.Replace("\\", "/");
    }

    public string GetFileUrl(string relativePath)
    {
        // Return URL path for serving through the application
        return $"/uploads/{relativePath.Replace("\\", "/")}";
    }

    public string GetPhysicalPath(string relativePath)
    {
        return Path.Combine(_uploadPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        var physicalPath = GetPhysicalPath(relativePath);

        if (File.Exists(physicalPath))
        {
            await Task.Run(() => File.Delete(physicalPath));
            _logger.LogInformation("File deleted: {Path}", relativePath);
        }
    }

    public bool FileExists(string relativePath)
    {
        var physicalPath = GetPhysicalPath(relativePath);
        return File.Exists(physicalPath);
    }

    public FileInfo? GetFileInfo(string relativePath)
    {
        var physicalPath = GetPhysicalPath(relativePath);
        return File.Exists(physicalPath) ? new FileInfo(physicalPath) : null;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file provided or file is empty");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }
    }

    private async Task ValidateFileContentAsync(string filePath, string expectedExtension)
    {
        if (!FileSignatures.TryGetValue(expectedExtension, out var expectedSignature))
        {
            return; // Skip validation for unknown types
        }

        var buffer = new byte[expectedSignature.Length];
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        await stream.ReadAsync(buffer.AsMemory(0, expectedSignature.Length));

        if (!buffer.Take(expectedSignature.Length).SequenceEqual(expectedSignature))
        {
            // Delete the file if content doesn't match extension
            File.Delete(filePath);
            throw new ArgumentException("File content does not match the declared file type");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalid.Contains(c)).ToArray());

        // Limit length
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50);
        }

        // Replace spaces with underscores
        sanitized = sanitized.Replace(" ", "_");

        // Ensure we have something
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "invoice";
        }

        return sanitized;
    }
}
