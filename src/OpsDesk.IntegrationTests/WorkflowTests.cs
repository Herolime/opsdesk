using System.Net;
using System.Net.Http.Json;
using OpsDesk.Api.Controllers;
using OpsDesk.Application.Dtos;
using OpsDesk.Application.Tickets.Commands;
using OpsDesk.Domain.Enums;

namespace OpsDesk.IntegrationTests;

public class WorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public WorkflowTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteTicketLifecycle_ShouldSucceedAndPersistDataCorrectly()
    {
        var client = _factory.CreateClient();

        // 1. Submit tickets with different priorities
        var urgentCommand = new SubmitRequestCommand
        {
            RequesterName = "Jordan Chen",
            RequesterEmail = "jordan.chen@opsdesk.io",
            Department = "Platform Engineering",
            Priority = Priority.Urgent,
            ServiceStream = ServiceStream.Infrastructure,
            Environment = DeploymentEnvironment.Production,
            Category = "Database & Caching",
            Title = "Redis cluster failover latency spike",
            Description = "Primary Redis node experiencing elevated memory fragmentation."
        };

        var highCommand = new SubmitRequestCommand
        {
            RequesterName = "Elena Rostova",
            RequesterEmail = "elena.rostova@opsdesk.io",
            Department = "Data Engineering",
            Priority = Priority.High,
            ServiceStream = ServiceStream.CloudPlatform,
            Environment = DeploymentEnvironment.Staging,
            Category = "Networking & VPC",
            Title = "Staging VPC peering route update",
            Description = "Need private CIDR route propagation between staging and analytics VPC."
        };

        var standardCommand = new SubmitRequestCommand
        {
            RequesterName = "Marcus Vance",
            RequesterEmail = "marcus.vance@opsdesk.io",
            Department = "Developer Experience",
            Priority = Priority.Standard,
            ServiceStream = ServiceStream.SecurityOps,
            Environment = DeploymentEnvironment.Development,
            Category = "IAM & SSO",
            Title = "Sandbox SSO role mapping",
            Description = "Add read-only developer group claim to sandbox portal."
        };

        var urgentResponse = await client.PostAsJsonAsync("/api/tickets", urgentCommand);
        Assert.Equal(HttpStatusCode.Created, urgentResponse.StatusCode);
        var urgentTicketId = (await urgentResponse.Content.ReadAsStringAsync()).Trim('"');
        Assert.StartsWith("OPS-", urgentTicketId);

        var highResponse = await client.PostAsJsonAsync("/api/tickets", highCommand);
        Assert.Equal(HttpStatusCode.Created, highResponse.StatusCode);
        var highTicketId = (await highResponse.Content.ReadAsStringAsync()).Trim('"');

        var standardResponse = await client.PostAsJsonAsync("/api/tickets", standardCommand);
        Assert.Equal(HttpStatusCode.Created, standardResponse.StatusCode);
        var standardTicketId = (await standardResponse.Content.ReadAsStringAsync()).Trim('"');

        // 2. Validate SLA target due dates (Urgent < High < Standard)
        var urgentDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        var highDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{highTicketId}");
        var standardDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{standardTicketId}");

        Assert.NotNull(urgentDetail);
        Assert.NotNull(highDetail);
        Assert.NotNull(standardDetail);
        Assert.Equal("Open", urgentDetail.Status);
        Assert.True(urgentDetail.TatDueDate > DateTime.UtcNow.AddMinutes(-5));
        Assert.True(highDetail.TatDueDate > urgentDetail.TatDueDate);
        Assert.True(standardDetail.TatDueDate > highDetail.TatDueDate);

        // 3. Verify Triage Queue & Specialist Suggestion
        var triageQueue = await client.GetFromJsonAsync<List<TriageTicketDto>>("/api/triage/pending");
        Assert.NotNull(triageQueue);
        Assert.Contains(triageQueue, t => t.Id == urgentTicketId);
        Assert.Contains(triageQueue, t => t.Id == highTicketId);
        Assert.Contains(triageQueue, t => t.Id == standardTicketId);

        var suggestion = await client.GetStringAsync(
            "/api/triage/specialist-suggestions?category=Database%20%26%20Caching&serviceStream=Infrastructure");
        Assert.Equal("Maya Lin", suggestion.Trim('"'));

        // 4. Assign Specialist -> Status transitions to InProgress
        var assignResponse = await client.PutAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/assign",
            new AssignSpecialistRequest("Maya Lin"));
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        var assignedDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.NotNull(assignedDetail);
        Assert.Equal("InProgress", assignedDetail.Status);
        Assert.Equal("Maya Lin", assignedDetail.AssignedSpecialist);

        // 5. Add Message & Internal Note
        var msgResponse = await client.PostAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/messages",
            new AddMessageRequest("Maya Lin", "Specialist", "Provisioning memory-optimized replica node."));
        Assert.Equal(HttpStatusCode.NoContent, msgResponse.StatusCode);

        var noteResponse = await client.PostAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/notes",
            new AddNoteRequest("Maya Lin", "Verified cutover window with SRE on-call."));
        Assert.Equal(HttpStatusCode.NoContent, noteResponse.StatusCode);

        var detailWithActivity = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.NotNull(detailWithActivity);
        Assert.Single(detailWithActivity.Messages);
        Assert.Single(detailWithActivity.Notes);

        // 6. Save Technical Review
        var reviewCommand = new SaveTechnicalReviewCommand
        {
            TicketId = urgentTicketId,
            ArchitectureReference = "RFC-042: Redis Cluster Topology",
            TechnicalAnalysis = "Replica promoted and memory tier upgraded to r6g.xlarge.",
            OperationalRisks = "Minimal risk during automated slot migration.",
            ResolutionSummary = "Failover validated with 0ms dropped writes.",
            ReviewedBy = "Maya Lin"
        };
        var reviewResponse = await client.PutAsJsonAsync($"/api/tickets/{urgentTicketId}/review", reviewCommand);
        Assert.Equal(HttpStatusCode.NoContent, reviewResponse.StatusCode);

        var detailWithReview = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.NotNull(detailWithReview?.Review);
        Assert.Equal("RFC-042: Redis Cluster Topology", detailWithReview.Review.ArchitectureReference);

        // 7. Escalate ticket
        var escalateResponse = await client.PostAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/escalate",
            new EscalateRequest("Database Reliability", "Requires cross-region replica validation."));
        Assert.Equal(HttpStatusCode.NoContent, escalateResponse.StatusCode);

        var detailWithEscalation = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.NotNull(detailWithEscalation?.Escalation);
        Assert.Equal("Database Reliability", detailWithEscalation.Escalation.EscalatedTeam);

        // 8. Resolve ticket & verify CompletedDate
        var resolveResponse = await client.PutAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/status",
            new UpdateStatusRequest(TicketStatus.Resolved));
        Assert.Equal(HttpStatusCode.NoContent, resolveResponse.StatusCode);

        var resolvedDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.NotNull(resolvedDetail);
        Assert.Equal("Resolved", resolvedDetail.Status);
        Assert.NotNull(resolvedDetail.CompletedDate);

        // 9. Verify Reports Summary
        var summary = await client.GetFromJsonAsync<ReportSummaryDto>("/api/reports/summary");
        Assert.NotNull(summary);
        Assert.Equal(3, summary.TotalTickets);
        Assert.Equal(2, summary.OpenTickets);
        Assert.Equal(0, summary.InProgressTickets);
        Assert.Equal(1, summary.ResolvedTickets);
        Assert.Equal(100.0, summary.OnTimeRate);
        Assert.Contains(summary.ByServiceStream, x => x.Label == "Infrastructure" && x.Count == 1);
        Assert.Contains(summary.ByServiceStream, x => x.Label == "CloudPlatform" && x.Count == 1);
        Assert.Contains(summary.ByServiceStream, x => x.Label == "SecurityOps" && x.Count == 1);
    }
}
