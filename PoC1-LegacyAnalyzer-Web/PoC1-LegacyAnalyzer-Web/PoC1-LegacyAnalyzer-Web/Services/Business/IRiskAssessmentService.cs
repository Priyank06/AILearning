namespace PoC1_LegacyAnalyzer_Web.Services.Business
{
    public interface IRiskAssessmentService
    {
        string DetermineRiskLevel(int complexityScore);
    }
}

