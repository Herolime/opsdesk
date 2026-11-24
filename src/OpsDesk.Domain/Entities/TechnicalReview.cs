namespace OpsDesk.Domain.Entities;

public sealed class TechnicalReview
{
    public string? ArchitectureReference { get; private set; }
    public string? TechnicalAnalysis { get; private set; }
    public string? OperationalRisks { get; private set; }
    public string? ResolutionSummary { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime UpdatedDate { get; private set; }

    public void Set(
        string? architectureReference,
        string? technicalAnalysis,
        string? operationalRisks,
        string? resolutionSummary,
        string? reviewedBy)
    {
        ArchitectureReference = architectureReference;
        TechnicalAnalysis = technicalAnalysis;
        OperationalRisks = operationalRisks;
        ResolutionSummary = resolutionSummary;
        ReviewedBy = reviewedBy;
        UpdatedDate = DateTime.UtcNow;
    }
}
