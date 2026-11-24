namespace OpsDesk.Application.Enums;

public enum TicketStatus
{
    Open,
    ReturnedToRequester,
    InProgress,
    PendingApproval,
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

public enum WorkspaceRole
{
    Requester,
    Specialist,
    TriageLead,
    OperationsLead,
    Admin
}
