using OpsDesk.Domain.Enums;

namespace OpsDesk.Web.Services;

public sealed class RoleState
{
    public WorkspaceRole CurrentRole { get; private set; } = WorkspaceRole.OperationsLead;

    public event Action? OnChange;

    public void SetRole(WorkspaceRole role)
    {
        if (CurrentRole != role)
        {
            CurrentRole = role;
            OnChange?.Invoke();
        }
    }

    public string CurrentRoleName => CurrentRole switch
    {
        WorkspaceRole.Requester => "Requester",
        WorkspaceRole.Specialist => "Engineering Specialist",
        WorkspaceRole.TriageLead => "Triage Lead",
        WorkspaceRole.OperationsLead => "Operations Lead",
        WorkspaceRole.Admin => "System Admin",
        _ => "Requester"
    };

    public string CurrentDisplayName => CurrentRole switch
    {
        WorkspaceRole.Requester => "Jordan Chen",
        WorkspaceRole.Specialist => "Maya Lin",
        WorkspaceRole.TriageLead => "Devon Brooks",
        WorkspaceRole.OperationsLead => "Alex Rivera",
        WorkspaceRole.Admin => "Samira Patel",
        _ => "Jordan Chen"
    };

    public string CurrentInitials => CurrentRole switch
    {
        WorkspaceRole.Requester => "JC",
        WorkspaceRole.Specialist => "ML",
        WorkspaceRole.TriageLead => "DB",
        WorkspaceRole.OperationsLead => "AR",
        WorkspaceRole.Admin => "SP",
        _ => "JC"
    };

    public string CurrentEmail => CurrentRole switch
    {
        WorkspaceRole.Requester => "jordan.chen@opsdesk.io",
        WorkspaceRole.Specialist => "maya.lin@opsdesk.io",
        WorkspaceRole.TriageLead => "devon.brooks@opsdesk.io",
        WorkspaceRole.OperationsLead => "alex.rivera@opsdesk.io",
        WorkspaceRole.Admin => "samira.patel@opsdesk.io",
        _ => "jordan.chen@opsdesk.io"
    };

    public string CurrentDepartment => CurrentRole switch
    {
        WorkspaceRole.Requester => "Platform Engineering",
        WorkspaceRole.Specialist => "Infrastructure SRE",
        WorkspaceRole.TriageLead => "Operations Triage",
        WorkspaceRole.OperationsLead => "Enterprise Operations",
        WorkspaceRole.Admin => "Platform Administration",
        _ => "Platform Engineering"
    };
}
