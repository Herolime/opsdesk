using MediatR;
using OpsDesk.Domain.Enums;

namespace OpsDesk.Application.Tickets.Commands;

public sealed record SubmitRequestCommand : IRequest<string>
{
    public string RequesterName { get; init; } = default!;
    public string RequesterEmail { get; init; } = default!;
    public string Department { get; init; } = default!;
    public Priority Priority { get; init; }
    public ServiceStream ServiceStream { get; init; }
    public DeploymentEnvironment Environment { get; init; } = DeploymentEnvironment.Production;
    public string Category { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string? TechnicalContext { get; init; }
}
