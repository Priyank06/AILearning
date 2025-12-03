namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IRiskAssessmentService
    {
        string DetermineRiskLevel(int complexityScore);
    }
}

