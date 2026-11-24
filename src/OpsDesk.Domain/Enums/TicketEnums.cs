namespace OpsDesk.Domain.Enums;

public enum TicketStatus
{
    Open,
    InProgress,
    PendingReview,
    Resolved
}

public enum Priority
{
    Urgent,
    High,
    Standard
}

public enum ServiceStream
{
    Infrastructure,
    CloudPlatform,
    SecurityOps,
    DataAnalytics
}

public enum DeploymentEnvironment
{
    Production,
    Staging,
    Development
}
