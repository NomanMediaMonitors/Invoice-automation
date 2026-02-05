using Microsoft.AspNetCore.Http;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Save an uploaded invoice file
    /// </summary>
    /// <param name="companyId">Company ID for organization</param>
    /// <param name="file">The uploaded file</param>
    /// <returns>Relative path to the saved file</returns>
    Task<string> SaveInvoiceFileAsync(Guid companyId, IFormFile file);

    /// <summary>
    /// Get the URL for serving a file
    /// </summary>
    /// <param name="relativePath">Relative path stored in database</param>
    /// <returns>URL to access the file</returns>
    string GetFileUrl(string relativePath);

    /// <summary>
    /// Get the physical path for a file
    /// </summary>
    /// <param name="relativePath">Relative path stored in database</param>
    /// <returns>Full physical path</returns>
    string GetPhysicalPath(string relativePath);

    /// <summary>
    /// Delete a file
    /// </summary>
    /// <param name="relativePath">Relative path to the file</param>
    Task DeleteFileAsync(string relativePath);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    /// <param name="relativePath">Relative path to the file</param>
    bool FileExists(string relativePath);

    /// <summary>
    /// Get file info
    /// </summary>
    /// <param name="relativePath">Relative path to the file</param>
    FileInfo? GetFileInfo(string relativePath);
}
