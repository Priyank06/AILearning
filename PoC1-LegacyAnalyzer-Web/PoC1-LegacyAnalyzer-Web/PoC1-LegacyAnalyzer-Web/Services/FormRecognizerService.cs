using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using PoC1_LegacyAnalyzer_Web.Models.AI102;
using System.Data;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class FormRecognizerService : IFormRecognizerService
    {
        private readonly DocumentAnalysisClient _client;
        private readonly ILogger<FormRecognizerService> _logger;

        public FormRecognizerService(IConfiguration configuration, ILogger<FormRecognizerService> logger)
        {
            var endpoint = configuration["CognitiveServices:FormRecognizer:Endpoint"] ??
                      configuration["CognitiveServices:DocumentIntelligence:Endpoint"] ??
                      Environment.GetEnvironmentVariable("FORM_RECOGNIZER_ENDPOINT");

            var apiKey = configuration["CognitiveServices:FormRecognizer:ApiKey"] ??
                        configuration["CognitiveServices:DocumentIntelligence:ApiKey"] ??
                        Environment.GetEnvironmentVariable("FORM_RECOGNIZER_KEY");

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Form Recognizer endpoint and API key must be configured");
            }

            _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _logger = logger;
        }

        public async Task<DocumentAnalysisResult> AnalyzeDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                var operation = await _client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-document",
                    documentStream);

                var result = operation.Value;

                return new DocumentAnalysisResult
                {
                    FileName = fileName,
                    ExtractedText = string.Join(" ", result.Pages.SelectMany(p => p.Lines?.Select(l => l.Content) ?? Enumerable.Empty<string>())),
                    KeyValuePairs = result.KeyValuePairs?.ToDictionary(kv => kv.Key.Content, kv => kv.Value?.Content ?? "") ?? new Dictionary<string, string>(),
                    Tables = result.Tables?.Select(MapToTableData).ToList() ?? new List<TableData>(),
                    ConfidenceScore = result.KeyValuePairs?.Any() == true ? result.KeyValuePairs.Average(kv => kv.Confidence) * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document {FileName}", fileName);
                throw;
            }
        }

        public async Task<List<ExtractedRequirement>> ExtractRequirementsAsync(Stream documentStream)
        {
            var analysis = await AnalyzeDocumentAsync(documentStream, "requirements.pdf");
            var requirements = new List<ExtractedRequirement>();

            // Simple requirement extraction logic - in real implementation, use more sophisticated NLP
            var lines = analysis.ExtractedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var requirementId = 1;

            foreach (var line in lines)
            {
                if (IsRequirementLine(line))
                {
                    requirements.Add(new ExtractedRequirement
                    {
                        RequirementId = $"REQ-{requirementId:D3}",
                        Title = ExtractTitle(line),
                        Description = line,
                        Type = DetermineRequirementType(line),
                        Priority = DeterminePriority(line),
                        Confidence = 0.8
                    });
                    requirementId++;
                }
            }

            return requirements;
        }

        public async Task<ArchitectureDiagramAnalysis> AnalyzeDiagramAsync(Stream imageStream)
        {
            // For diagram analysis, we'd typically use Computer Vision + custom logic
            // This is a simplified implementation
            var analysis = await AnalyzeDocumentAsync(imageStream, "diagram.png");

            return new ArchitectureDiagramAnalysis
            {
                Components = new List<DiagramComponent>(), // Would be populated from image analysis
                Connections = new List<DiagramConnection>(),
                DiagramType = DiagramType.ArchitectureDiagram,
                ConfidenceScore = analysis.ConfidenceScore,
                Description = analysis.ExtractedText
            };
        }

        private TableData MapToTableData(DocumentTable table)
        {
            var cells = new List<List<string>>();
            for (int row = 0; row < table.RowCount; row++)
            {
                var rowCells = new List<string>();
                for (int col = 0; col < table.ColumnCount; col++)
                {
                    var cell = table.Cells.FirstOrDefault(c => c.RowIndex == row && c.ColumnIndex == col);
                    rowCells.Add(cell?.Content ?? "");
                }
                cells.Add(rowCells);
            }

            return new TableData
            {
                RowCount = table.RowCount,
                ColumnCount = table.ColumnCount,
                Cells = cells,
                Confidence = 0.9 // Simplified confidence calculation
            };
        }

        private bool IsRequirementLine(string line)
        {
            return line.Contains("shall") || line.Contains("must") || line.Contains("should") ||
                   line.StartsWith("REQ") || line.Contains("requirement");
        }

        private string ExtractTitle(string line)
        {
            return line.Length > 50 ? line.Substring(0, 50) + "..." : line;
        }

        private RequirementType DetermineRequirementType(string line)
        {
            if (line.ToLower().Contains("performance") || line.ToLower().Contains("security"))
                return RequirementType.NonFunctional;
            return RequirementType.Functional;
        }

        private int DeterminePriority(string line)
        {
            if (line.ToLower().Contains("critical") || line.ToLower().Contains("must"))
                return 1;
            if (line.ToLower().Contains("important") || line.ToLower().Contains("should"))
                return 2;
            return 3;
        }
    }
}
