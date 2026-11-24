using Dapper;
using OpsDesk.Application.Common.Interfaces;

namespace OpsDesk.Infrastructure.Persistence.Db;

public static class OpsDeskSchemaBaseline
{
    public static void Initialize(IDbConnectionFactory factory)
    {
        using var connection = factory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();
    }

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Tickets (
            Id TEXT PRIMARY KEY,
            CreatedDate TEXT NOT NULL,
            Priority TEXT NOT NULL,
            RequesterName TEXT NOT NULL,
            RequesterEmail TEXT NOT NULL,
            Department TEXT NOT NULL,
            ServiceStream TEXT NOT NULL,
            Environment TEXT NOT NULL,
            Category TEXT NOT NULL,
            Title TEXT NOT NULL,
            Description TEXT NOT NULL,
            TechnicalContext TEXT,
            Status TEXT NOT NULL,
            AssignedSpecialist TEXT,
            ReturnReason TEXT,
            ReturnedBy TEXT,
            ReturnedAt TEXT,
            TargetTatDays INTEGER NOT NULL,
            TatDueDate TEXT NOT NULL,
            CompletedDate TEXT,
            ClosedBy TEXT
        );

        CREATE TABLE IF NOT EXISTS TechnicalReviews (
            TicketId TEXT PRIMARY KEY REFERENCES Tickets(Id),
            ArchitectureReference TEXT,
            TechnicalAnalysis TEXT,
            OperationalRisks TEXT,
            ResolutionSummary TEXT,
            ReviewedBy TEXT,
            UpdatedDate TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS TicketMessages (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketId TEXT NOT NULL REFERENCES Tickets(Id),
            SenderName TEXT NOT NULL,
            SenderRole TEXT NOT NULL,
            MessageText TEXT NOT NULL,
            CreatedDate TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS TicketNotes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketId TEXT NOT NULL REFERENCES Tickets(Id),
            Author TEXT NOT NULL,
            NoteText TEXT NOT NULL,
            CreatedDate TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Escalations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketId TEXT NOT NULL REFERENCES Tickets(Id),
            EscalatedTeam TEXT NOT NULL,
            EscalationReason TEXT,
            EscalatedDate TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS StreamSpecialistMappings (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CategoryName TEXT NOT NULL,
            ServiceStream TEXT NOT NULL,
            PrimarySpecialist TEXT NOT NULL,
            BackupSpecialist TEXT,
            UNIQUE(CategoryName, ServiceStream)
        );

        CREATE TABLE IF NOT EXISTS Notifications (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketId TEXT NOT NULL,
            RecipientEmail TEXT NOT NULL,
            Title TEXT NOT NULL,
            Message TEXT NOT NULL,
            Category TEXT NOT NULL,
            IsRead INTEGER NOT NULL DEFAULT 0,
            CreatedDate TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Sequence (
            Id INTEGER PRIMARY KEY CHECK (Id = 1),
            LastValue INTEGER NOT NULL
        );

        INSERT OR IGNORE INTO Sequence (Id, LastValue) VALUES (1, 2040);
        """;

    public static void SeedStreamMappings(IDbConnectionFactory factory)
    {
        using var connection = factory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO StreamSpecialistMappings (CategoryName, ServiceStream, PrimarySpecialist, BackupSpecialist) VALUES
            ('Kubernetes & Compute', 'Infrastructure', 'Maya Lin', 'Devon Brooks'),
            ('Database & Caching', 'Infrastructure', 'Maya Lin', 'Samira Patel'),
            ('Networking & VPC', 'CloudPlatform', 'Devon Brooks', 'Maya Lin'),
            ('CI/CD & Pipelines', 'CloudPlatform', 'Devon Brooks', 'Lucas Vance'),
            ('IAM & SSO', 'SecurityOps', 'Samira Patel', 'Alex Rivera'),
            ('Secrets & Certificates', 'SecurityOps', 'Samira Patel', 'Devon Brooks'),
            ('Data Warehouse & ETL', 'DataAnalytics', 'Lucas Vance', 'Maya Lin'),
            ('Observability & Telemetry', 'CloudPlatform', 'Lucas Vance', 'Samira Patel'),
            ('General Operations', 'Infrastructure', 'Triage Queue', NULL);
            """;
        command.ExecuteNonQuery();
    }

    public static void SeedSampleTickets(IDbConnectionFactory factory)
    {
        using var connection = factory.CreateConnection();
        var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tickets");
        if (count > 0) return;

        var now = DateTime.UtcNow;

        connection.Execute("""
            INSERT INTO Tickets (
                Id, CreatedDate, Priority, RequesterName, RequesterEmail, Department,
                ServiceStream, Environment, Category, Title, Description, TechnicalContext,
                Status, AssignedSpecialist, ReturnReason, ReturnedBy, ReturnedAt,
                TargetTatDays, TatDueDate, CompletedDate, ClosedBy
            ) VALUES
            ('OPS-2041', @D1, 'Urgent', 'Jordan Chen', 'jordan.chen@opsdesk.io', 'Platform Engineering', 'Infrastructure', 'Production', 'Database & Caching', 'Production Redis Cluster Memory Expansion & Failover Test', 'Primary Redis cluster in us-east-1 is sustained at 86% memory utilization during peak traffic. Need memory tier upgrade and failover verification.', 'Cluster ID: redis-prod-01 | Current node type: cache.r6g.large', 'InProgress', 'Maya Lin', NULL, NULL, NULL, 1, @Due1, NULL, NULL),
            ('OPS-2042', @D2, 'High', 'Elena Rostova', 'elena.rostova@opsdesk.io', 'Data Engineering', 'CloudPlatform', 'Staging', 'Networking & VPC', 'Staging Environment VPC Peering for Analytics Cluster', 'Establish private VPC peering between staging-app-vpc and analytics-warehouse-vpc with restricted CIDR routing.', 'CIDR 10.42.0.0/16 <-> 10.88.0.0/16', 'Open', NULL, NULL, NULL, NULL, 3, @Due2, NULL, NULL),
            ('OPS-2043', @D3, 'Standard', 'Jordan Chen', 'jordan.chen@opsdesk.io', 'Platform Engineering', 'SecurityOps', 'Production', 'IAM & SSO', 'SSO SAML Provisioning for Internal Telemetry Portal', 'Configure Okta SAML 2.0 application integration and RBAC group claims for the new observability dashboard.', 'Group claims: ops-eng, ops-leads', 'PendingApproval', 'Samira Patel', NULL, NULL, NULL, 7, @Due3, NULL, NULL),
            ('OPS-2044', @D4, 'High', 'Marcus Vance', 'marcus.vance@opsdesk.io', 'Developer Experience', 'CloudPlatform', 'Production', 'CI/CD & Pipelines', 'CI/CD Self-Hosted Runner Autoscale Quota Increase', 'Build queue wait times exceeded 12 minutes during release windows. Requesting runner pool max-concurrency bump from 20 to 45.', 'GitHub Actions ARC runner-set-prod', 'InProgress', 'Devon Brooks', NULL, NULL, NULL, 3, @Due4, NULL, NULL),
            ('OPS-2045', @D5, 'Urgent', 'Jordan Chen', 'jordan.chen@opsdesk.io', 'Platform Engineering', 'SecurityOps', 'Production', 'Secrets & Certificates', 'Production Kubernetes Ingress TLS Certificate Renewal', 'Automated cert-manager ACME challenge failed due to DNS TXT propagation delay. Manual validation needed before expiry.', 'Namespace: ingress-nginx | Domain: api.opsdesk.io', 'ReturnedToRequester', NULL, 'Please attach the DNS zone ID and confirm whether wildcard SANs are required.', 'Devon Brooks', @Ret5, 1, @Due5, NULL, NULL),
            ('OPS-2046', @D6, 'Standard', 'Priya Nair', 'priya.nair@opsdesk.io', 'Analytics', 'DataAnalytics', 'Production', 'Data Warehouse & ETL', 'Read-Only Warehouse Replica Access for BI Service', 'Provision least-privilege read-only service role for quarterly SLA reporting pipelines.', 'Target schema: ops_metrics_ro', 'Resolved', 'Lucas Vance', NULL, NULL, NULL, 7, @Due6, @Done6, 'Alex Rivera');
            """,
            new
            {
                D1 = now.AddDays(-2).ToString("O"),
                Due1 = now.AddHours(8).ToString("O"),
                D2 = now.AddHours(-6).ToString("O"),
                Due2 = now.AddDays(3).ToString("O"),
                D3 = now.AddDays(-3).ToString("O"),
                Due3 = now.AddDays(4).ToString("O"),
                D4 = now.AddDays(-1).ToString("O"),
                Due4 = now.AddDays(2).ToString("O"),
                D5 = now.AddHours(-2).ToString("O"),
                Ret5 = now.AddHours(-1).ToString("O"),
                Due5 = now.AddDays(1).ToString("O"),
                D6 = now.AddDays(-5).ToString("O"),
                Due6 = now.AddDays(2).ToString("O"),
                Done6 = now.AddDays(-1).ToString("O")
            });

        connection.Execute("""
            INSERT INTO TicketMessages (TicketId, SenderName, SenderRole, MessageText, CreatedDate) VALUES
            ('OPS-2041', 'Jordan Chen', 'Requester', 'Memory pressure spiked again at 14:00 UTC. Attaching peak keyspace metrics for review.', @M1),
            ('OPS-2041', 'Maya Lin', 'Specialist', 'Acknowledged. Provisioning r6g.xlarge replica nodes in staging first to validate zero-downtime failover.', @M2),
            ('OPS-2044', 'Devon Brooks', 'Specialist', 'Increased warm pool min-replicas to 10; monitoring runner startup latency.', @M3);
            """,
            new
            {
                M1 = now.AddHours(-18).ToString("O"),
                M2 = now.AddHours(-10).ToString("O"),
                M3 = now.AddHours(-5).ToString("O")
            });

        connection.Execute("""
            INSERT INTO TicketNotes (TicketId, Author, NoteText, CreatedDate) VALUES
            ('OPS-2041', 'Maya Lin', 'Confirmed maintenance window with SRE on-call for 02:00 UTC cutover.', @N1);
            """,
            new { N1 = now.AddHours(-9).ToString("O") });

        connection.Execute("""
            INSERT INTO Notifications (TicketId, RecipientEmail, Title, Message, Category, IsRead, CreatedDate) VALUES
            ('OPS-2045', 'jordan.chen@opsdesk.io', 'Action Required: Ticket Returned', 'OPS-2045 was returned by Devon Brooks: Please attach the DNS zone ID and confirm whether wildcard SANs are required.', 'Returned', 0, @N1),
            ('OPS-2043', 'alex.rivera@opsdesk.io', 'Pending Leader Approval', 'OPS-2043 technical analysis was submitted by Samira Patel for approval.', 'Approval', 0, @N2),
            ('OPS-2041', 'maya.lin@opsdesk.io', 'SLA Threshold Alert', 'OPS-2041 is within 8 hours of target resolution.', 'SLA', 0, @N3);
            """,
            new
            {
                N1 = now.AddHours(-1).ToString("O"),
                N2 = now.AddHours(-3).ToString("O"),
                N3 = now.AddHours(-2).ToString("O")
            });

        connection.Execute("UPDATE Sequence SET LastValue = 2046 WHERE Id = 1;");
    }
}
