using Microsoft.JSInterop;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class FileDownloadService : IFileDownloadService
    {
        private readonly IJSRuntime _jsRuntime;

        public FileDownloadService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task DownloadFileAsync(string fileName, byte[] fileContent, string contentType = "text/markdown")
        {
            var base64 = Convert.ToBase64String(fileContent);
            var dataUrl = $"data:{contentType};base64,{base64}";

            await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, dataUrl);
        }
    }
}
