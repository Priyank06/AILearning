namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Defines a service for downloading files in a Blazor application.
    /// </summary>
    public interface IFileDownloadService
    {
        /// <summary>
        /// Initiates a file download with the specified file name, content, and content type.
        /// </summary>
        /// <param name="fileName">The name of the file to be downloaded.</param>
        /// <param name="fileContent">The byte array representing the file content.</param>
        /// <param name="contentType">The MIME type of the file. Defaults to "text/markdown".</param>
        /// <returns>A task representing the asynchronous download operation.</returns>
        Task DownloadFileAsync(string fileName, byte[] fileContent, string contentType = "text/markdown");
    }
}

