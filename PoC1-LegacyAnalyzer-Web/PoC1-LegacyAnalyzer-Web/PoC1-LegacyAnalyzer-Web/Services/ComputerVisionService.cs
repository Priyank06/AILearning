using Azure.AI.Vision.ImageAnalysis;
using Azure;
using PoC1_LegacyAnalyzer_Web.Models.AI102;
using AzureDetectedObject = Azure.AI.Vision.ImageAnalysis.DetectedObject;
using CustomDetectedObject = PoC1_LegacyAnalyzer_Web.Models.AI102.DetectedObject;
using AzureImageAnalysisResult = Azure.AI.Vision.ImageAnalysis.ImageAnalysisResult;
using CustomImageAnalysisResult = PoC1_LegacyAnalyzer_Web.Models.AI102.ImageAnalysisResult;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class ComputerVisionService : IComputerVisionService
    {
        private readonly ImageAnalysisClient _client;
        private readonly ILogger<ComputerVisionService> _logger;

        public ComputerVisionService(IConfiguration configuration, ILogger<ComputerVisionService> logger)
        {
            var endpoint = configuration["CognitiveServices:ComputerVision:Endpoint"] ??
                          Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT");
            var apiKey = configuration["CognitiveServices:ComputerVision:ApiKey"] ??
                        Environment.GetEnvironmentVariable("COMPUTER_VISION_KEY");

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                _client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            }
            _logger = logger;
        }

        public async Task<CustomImageAnalysisResult> AnalyzeArchitectureDiagramAsync(Stream imageStream)
        {
            if (_client == null)
            {
                _logger.LogWarning("Computer Vision not configured");
                return new CustomImageAnalysisResult();
            }

            try
            {
                var binaryData = BinaryData.FromStream(imageStream);
                var result = await _client.AnalyzeAsync(
                    binaryData,
                    VisualFeatures.Caption | VisualFeatures.Objects | VisualFeatures.Read | VisualFeatures.Tags);

                return new CustomImageAnalysisResult
                {
                    Description = result.Value.Caption?.Text ?? "No description available",
                    Tags = result.Value.Tags?.Values?.Select(t => t.Name).ToList() ?? new List<string>(),
                    Objects = MapAzureObjectsToCustomObjects(result.Value.Objects?.Values?.ToList() ?? new List<AzureDetectedObject>()),
                    Confidence = result.Value.Caption?.Confidence ?? 0,
                    ExtractedText = result.Value.Read?.Blocks?.SelectMany(b => b.Lines)?.Select(l => l.Text).ToList() ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing architecture diagram");
                return new CustomImageAnalysisResult();
            }
        }

        public async Task<UIAnalysisResult> AnalyzeUIDesignAsync(Stream imageStream)
        {
            var imageAnalysis = await AnalyzeArchitectureDiagramAsync(imageStream);

            return new UIAnalysisResult
            {
                Elements = imageAnalysis.Objects.Select(obj => new UIElement
                {
                    ElementType = DetermineUIElementType(obj.Name),
                    Text = ExtractTextFromObject(obj, imageAnalysis.ExtractedText),
                    BoundingBox = obj.BoundingBox,
                    Confidence = obj.Confidence
                }).ToList(),
                Layout = DetermineLayoutType(imageAnalysis.Objects),
                Confidence = imageAnalysis.Confidence,
                Description = imageAnalysis.Description
            };
        }

        public async Task<List<string>> ExtractTextFromImageAsync(Stream imageStream)
        {
            var analysis = await AnalyzeArchitectureDiagramAsync(imageStream);
            return analysis.ExtractedText;
        }

        public async Task<DiagramComponentsResult> IdentifyDiagramComponentsAsync(Stream imageStream)
        {
            var analysis = await AnalyzeArchitectureDiagramAsync(imageStream);

            return new DiagramComponentsResult
            {
                Components = analysis.Objects.Select(obj => new DiagramComponent
                {
                    Name = obj.Name,
                    Type = DetermineComponentType(obj.Name),
                    BoundingBox = obj.BoundingBox,
                    Confidence = obj.Confidence
                }).ToList(),
                DiagramType = DetermineDiagramType(analysis.Tags, analysis.Description),
                OverallConfidence = analysis.Confidence,
                Metadata = new Dictionary<string, object>
                {
                    ["ExtractedText"] = analysis.ExtractedText,
                    ["Tags"] = analysis.Tags,
                    ["Description"] = analysis.Description
                }
            };
        }

        public async Task<string> AnalyzeProjectDiagramAsync(string imageUrl)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(imageUrl);
            using var stream = await response.Content.ReadAsStreamAsync();

            var analysis = await AnalyzeArchitectureDiagramAsync(stream);
            return $"Diagram Analysis: {analysis.Description}. Found {analysis.Objects.Count} components.";
        }

        public async Task<List<string>> DetectUIElementsAsync(string imageUrl)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(imageUrl);
            using var stream = await response.Content.ReadAsStreamAsync();

            var uiAnalysis = await AnalyzeUIDesignAsync(stream);
            return uiAnalysis.Elements.Select(e => $"{e.ElementType}: {e.Text}").ToList();
        }

        public async Task<List<string>> ExtractTextFromDocumentAsync(string documentPath)
        {
            if (File.Exists(documentPath))
            {
                using var fileStream = File.OpenRead(documentPath);
                return await ExtractTextFromImageAsync(fileStream);
            }
            return new List<string>();
        }

        // Helper method to map Azure objects to custom objects
        private List<CustomDetectedObject> MapAzureObjectsToCustomObjects(List<AzureDetectedObject> azureObjects)
        {
            return azureObjects.Select(azureObj => new CustomDetectedObject
            {
                Name = azureObj.Tags?.FirstOrDefault()?.Name ?? "Unknown",
                Confidence = azureObj.Tags?.FirstOrDefault()?.Confidence ?? 0,
                BoundingBox = new Models.AI102.BoundingBox
                {
                    X = azureObj.BoundingBox?.X ?? 0,
                    Y = azureObj.BoundingBox?.Y ?? 0,
                    Width = azureObj.BoundingBox?.Width ?? 0,
                    Height = azureObj.BoundingBox?.Height ?? 0
                }
            }).ToList();
        }

        private string DetermineUIElementType(string objectName)
        {
            return objectName.ToLower() switch
            {
                var name when name.Contains("button") => "Button",
                var name when name.Contains("text") => "TextBox",
                var name when name.Contains("image") => "Image",
                var name when name.Contains("menu") => "Menu",
                _ => "Unknown"
            };
        }

        private string ExtractTextFromObject(CustomDetectedObject obj, List<string> allText)
        {
            return allText.FirstOrDefault(t => t.Length > 0) ?? "";
        }

        private UILayoutType DetermineLayoutType(List<CustomDetectedObject> objects)
        {
            if (objects.Count == 0) return UILayoutType.Unknown;

            var sortedByY = objects.OrderBy(o => o.BoundingBox.Y).ToList();
            var yVariation = sortedByY.Last().BoundingBox.Y - sortedByY.First().BoundingBox.Y;

            return yVariation > 200 ? UILayoutType.Stack : UILayoutType.Grid;
        }

        private ComponentType DetermineComponentType(string objectName)
        {
            return objectName.ToLower() switch
            {
                var name when name.Contains("database") || name.Contains("db") => ComponentType.Database,
                var name when name.Contains("service") || name.Contains("api") => ComponentType.Service,
                var name when name.Contains("ui") || name.Contains("interface") => ComponentType.UserInterface,
                var name when name.Contains("process") => ComponentType.Process,
                _ => ComponentType.Unknown
            };
        }

        private DiagramType DetermineDiagramType(List<string> tags, string description)
        {
            var combined = string.Join(" ", tags) + " " + description;
            combined = combined.ToLower();

            if (combined.Contains("architecture") || combined.Contains("system"))
                return DiagramType.ArchitectureDiagram;
            if (combined.Contains("flow") || combined.Contains("process"))
                return DiagramType.FlowChart;
            if (combined.Contains("database") || combined.Contains("entity"))
                return DiagramType.EntityRelationship;
            if (combined.Contains("network"))
                return DiagramType.NetworkDiagram;
            if (combined.Contains("ui") || combined.Contains("wireframe"))
                return DiagramType.UIWireframe;

            return DiagramType.Unknown;
        }
    }
}