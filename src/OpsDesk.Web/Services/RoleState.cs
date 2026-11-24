using Microsoft.JSInterop;
using OpsDesk.Domain.Enums;

namespace OpsDesk.Web.Services;

public sealed class RoleState
{
    private const string StorageKey = "opsdesk_active_role";
    private readonly IJSRuntime _js;
    private bool _initialized;

    public RoleState(IJSRuntime js) => _js = js;

    public WorkspaceRole CurrentRole { get; private set; } = WorkspaceRole.OperationsLead;

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            var saved = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(saved) && Enum.TryParse<WorkspaceRole>(saved, true, out var parsed) && parsed != CurrentRole)
            {
                CurrentRole = parsed;
                OnChange?.Invoke();
            }
        }
        catch
        {
            // Ignore JS interop errors during prerendering or unsupported hosts
        }
    }

    public void SetRole(WorkspaceRole role)
    {
        if (CurrentRole != role)
        {
            CurrentRole = role;
            OnChange?.Invoke();
        }
    }

    public async Task SetRoleAsync(WorkspaceRole role)
    {
        SetRole(role);
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, role.ToString());
        }
        catch
        {
            // Ignore JS interop errors
        }
    }

    public bool IsInRole(params WorkspaceRole[] roles) => roles.Contains(CurrentRole);

    public bool IsOperationalStaff => CurrentRole is
        WorkspaceRole.Specialist or
        WorkspaceRole.TriageLead or
        WorkspaceRole.OperationsLead or
        WorkspaceRole.Admin;

    public bool CanAccessTriage => CurrentRole is
        WorkspaceRole.TriageLead or
        WorkspaceRole.OperationsLead or
        WorkspaceRole.Admin;

    public bool CanAccessReports => IsOperationalStaff;

    public bool CanAccessAllTickets => IsOperationalStaff;

    public bool CanAccessAdmin => CurrentRole is WorkspaceRole.Admin;

    public string DefaultRoute => CurrentRole == WorkspaceRole.Requester ? "/my-requests" : "/";

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
