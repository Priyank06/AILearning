namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IFileDownloadService
    {
        Task DownloadFileAsync(string fileName, byte[] fileContent, string contentType = "text/markdown");
    }
}
