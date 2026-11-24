using System.Net;
using System.Net.Http.Json;
using OpsDesk.Api.Controllers;
using OpsDesk.Application.Dtos;
using OpsDesk.Application.Entities;
using OpsDesk.Application.Enums;
using OpsDesk.Application.Tickets.Commands;

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

        // 3. Return High ticket to requester, then resubmit
        var returnResp = await client.PostAsJsonAsync(
            $"/api/tickets/{highTicketId}/return-to-requester",
            new ReturnToRequesterRequest("Devon Brooks", "Please specify target CIDR blocks."));
        Assert.Equal(HttpStatusCode.NoContent, returnResp.StatusCode);

        var returnedDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{highTicketId}");
        Assert.NotNull(returnedDetail);
        Assert.Equal("ReturnedToRequester", returnedDetail.Status);
        Assert.Equal("Please specify target CIDR blocks.", returnedDetail.ReturnReason);

        var resubmitResp = await client.PutAsJsonAsync(
            $"/api/tickets/{highTicketId}/resubmit",
            new ResubmitRequest(highCommand.Title, "Updated with CIDR 10.42.0.0/16 <-> 10.88.0.0/16.", "CIDR verified"));
        Assert.Equal(HttpStatusCode.NoContent, resubmitResp.StatusCode);

        var resubmittedDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{highTicketId}");
        Assert.NotNull(resubmittedDetail);
        Assert.Equal("Open", resubmittedDetail.Status);
        Assert.Null(resubmittedDetail.ReturnReason);

        // 4. Verify Triage Queue & Specialist Suggestion
        var triageQueue = await client.GetFromJsonAsync<List<TriageTicketDto>>("/api/triage/pending");
        Assert.NotNull(triageQueue);
        Assert.Contains(triageQueue, t => t.Id == urgentTicketId);

        var suggestion = await client.GetStringAsync(
            "/api/triage/specialist-suggestions?category=Database%20%26%20Caching&serviceStream=Infrastructure");
        Assert.Equal("Maya Lin", suggestion.Trim('"'));

        // 5. Assign Specialist -> Status transitions to InProgress
        var assignResponse = await client.PutAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/assign",
            new AssignSpecialistRequest("Maya Lin"));
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        // 6. Save Technical Review, Submit for Leader Approval, Return to Progress, Re-submit, Approve & Close
        var reviewCommand = new SaveTechnicalReviewCommand
        {
            TicketId = urgentTicketId,
            ArchitectureReference = "RFC-042: Redis Cluster Topology",
            TechnicalAnalysis = "<p>Replica promoted and memory tier upgraded to <strong>r6g.xlarge</strong>.</p>",
            OperationalRisks = "<p>Minimal risk during automated slot migration.</p>",
            ResolutionSummary = "<p>Failover validated with 0ms dropped writes.</p>",
            ReviewedBy = "Maya Lin"
        };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/tickets/{urgentTicketId}/review", reviewCommand)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/submit-for-approval",
            new SubmitForApprovalRequest("Maya Lin"))).StatusCode);

        var pendingApproval = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.Equal("PendingApproval", pendingApproval?.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/return-to-progress",
            new ReturnToProgressRequest("Alex Rivera", "Add failover latency benchmark."))).StatusCode);

        var backInProgress = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.Equal("InProgress", backInProgress?.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/submit-for-approval",
            new SubmitForApprovalRequest("Maya Lin"))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"/api/tickets/{urgentTicketId}/approve-close",
            new ApproveCloseRequest("Alex Rivera", "Approved and closed after benchmark verification."))).StatusCode);

        var resolvedDetail = await client.GetFromJsonAsync<TicketDetailDto>($"/api/tickets/{urgentTicketId}");
        Assert.NotNull(resolvedDetail);
        Assert.Equal("Resolved", resolvedDetail.Status);
        Assert.Equal("Alex Rivera", resolvedDetail.ClosedBy);
        Assert.NotNull(resolvedDetail.CompletedDate);

        // 7. Verify Notifications & Reports Summary
        var notifications = await client.GetFromJsonAsync<List<Notification>>("/api/notifications");
        Assert.NotNull(notifications);
        Assert.NotEmpty(notifications);

        var summary = await client.GetFromJsonAsync<ReportSummaryDto>("/api/reports/summary");
        Assert.NotNull(summary);
        Assert.Equal(3, summary.TotalTickets);
        Assert.Equal(1, summary.ResolvedTickets);
        Assert.Equal(100.0, summary.OnTimeRate);
    }
}
