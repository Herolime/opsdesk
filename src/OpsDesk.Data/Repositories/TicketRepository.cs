using System.Globalization;
using Dapper;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Domain.Aggregates;
using OpsDesk.Domain.Entities;
using OpsDesk.Domain.Enums;
using OpsDesk.Domain.ValueObjects;

namespace OpsDesk.Data.Repositories;

public sealed class TicketRepository : ITicketRepository, ITicketNumberGenerator
{
    private readonly IDbConnectionFactory _factory;

    public TicketRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<TicketId> NextAsync(CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        using var tx = connection.BeginTransaction();
        var next = await connection.ExecuteScalarAsync<int>(
            "UPDATE Sequence SET LastValue = LastValue + 1 WHERE Id = 1 RETURNING LastValue;",
            transaction: tx);
        tx.Commit();
        return TicketId.Create(next);
    }

    public async Task<ServiceTicket?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<TicketRow>(
            "SELECT * FROM Tickets WHERE Id = @Id", new { Id = id });

        if (row is null) return null;

        var reviewRow = await connection.QuerySingleOrDefaultAsync<TechnicalReviewRow>(
            "SELECT * FROM TechnicalReviews WHERE TicketId = @Id", new { Id = id });

        TechnicalReview? review = null;
        if (reviewRow is not null)
        {
            review = new TechnicalReview();
            review.Set(
                reviewRow.ArchitectureReference,
                reviewRow.TechnicalAnalysis,
                reviewRow.OperationalRisks,
                reviewRow.ResolutionSummary,
                reviewRow.ReviewedBy);
        }

        var escalationRow = await connection.QuerySingleOrDefaultAsync<EscalationRow>(
            "SELECT * FROM Escalations WHERE TicketId = @Id ORDER BY Id DESC LIMIT 1", new { Id = id });

        var escalation = escalationRow is null
            ? null
            : new Escalation(escalationRow.EscalatedTeam, escalationRow.EscalationReason);

        var msgRows = await connection.QueryAsync<MessageRow>(
            "SELECT * FROM TicketMessages WHERE TicketId = @Id ORDER BY Id ASC", new { Id = id });
        var messages = msgRows.Select(m => new TicketMessage(
            m.Id, m.SenderName, m.SenderRole, m.MessageText,
            DateTime.Parse(m.CreatedDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));

        var noteRows = await connection.QueryAsync<NoteRow>(
            "SELECT * FROM TicketNotes WHERE TicketId = @Id ORDER BY Id ASC", new { Id = id });
        var notes = noteRows.Select(n => new TicketNote(
            n.Id, n.Author, n.NoteText,
            DateTime.Parse(n.CreatedDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));

        var ticket = ServiceTicket.Create(
            TicketId.Create(row.Id),
            new RequesterInfo(row.RequesterName, row.RequesterEmail, row.Department),
            Enum.Parse<Priority>(row.Priority, ignoreCase: true),
            Enum.Parse<ServiceStream>(row.ServiceStream, ignoreCase: true),
            Enum.Parse<DeploymentEnvironment>(row.Environment, ignoreCase: true),
            row.Category,
            row.Title,
            row.Description,
            row.TechnicalContext);

        ticket.ClearDomainEvents();
        ticket.Hydrate(
            DateTime.Parse(row.CreatedDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Enum.Parse<TicketStatus>(row.Status, ignoreCase: true),
            row.AssignedSpecialist,
            DateTime.Parse(row.TatDueDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            row.TargetTatDays,
            row.CompletedDate is null
                ? null
                : DateTime.Parse(row.CompletedDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            review,
            escalation,
            messages,
            notes);

        return ticket;
    }

    public async Task AddAsync(ServiceTicket ticket, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync("""
            INSERT INTO Tickets (
                Id, CreatedDate, Priority, RequesterName, RequesterEmail, Department,
                ServiceStream, Environment, Category, Title, Description, TechnicalContext,
                Status, AssignedSpecialist, TargetTatDays, TatDueDate, CompletedDate
            ) VALUES (
                @Id, @CreatedDate, @Priority, @RequesterName, @RequesterEmail, @Department,
                @ServiceStream, @Environment, @Category, @Title, @Description, @TechnicalContext,
                @Status, @AssignedSpecialist, @TargetTatDays, @TatDueDate, @CompletedDate
            )
            """,
            new
            {
                Id = ticket.Id.Value,
                CreatedDate = ticket.CreatedDate.ToString("O"),
                Priority = ticket.Priority.ToString(),
                RequesterName = ticket.Requester.Name,
                RequesterEmail = ticket.Requester.Email,
                Department = ticket.Requester.Department,
                ServiceStream = ticket.ServiceStream.ToString(),
                Environment = ticket.Environment.ToString(),
                ticket.Category,
                ticket.Title,
                ticket.Description,
                ticket.TechnicalContext,
                Status = ticket.Status.ToString(),
                ticket.AssignedSpecialist,
                ticket.Sla.TargetTatDays,
                TatDueDate = ticket.Sla.TatDueDate.ToString("O"),
                CompletedDate = ticket.CompletedDate?.ToString("O")
            }, tx);

        tx.Commit();
        ticket.ClearDomainEvents();
    }

    public async Task UpdateAsync(ServiceTicket ticket, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync("""
            UPDATE Tickets SET
                Priority = @Priority,
                Status = @Status,
                AssignedSpecialist = @AssignedSpecialist,
                TargetTatDays = @TargetTatDays,
                TatDueDate = @TatDueDate,
                CompletedDate = @CompletedDate
            WHERE Id = @Id
            """,
            new
            {
                Id = ticket.Id.Value,
                Priority = ticket.Priority.ToString(),
                Status = ticket.Status.ToString(),
                ticket.AssignedSpecialist,
                ticket.Sla.TargetTatDays,
                TatDueDate = ticket.Sla.TatDueDate.ToString("O"),
                CompletedDate = ticket.CompletedDate?.ToString("O")
            }, tx);

        if (ticket.Review is not null)
        {
            await connection.ExecuteAsync("""
                INSERT INTO TechnicalReviews (TicketId, ArchitectureReference, TechnicalAnalysis, OperationalRisks, ResolutionSummary, ReviewedBy, UpdatedDate)
                VALUES (@TicketId, @ArchitectureReference, @TechnicalAnalysis, @OperationalRisks, @ResolutionSummary, @ReviewedBy, @UpdatedDate)
                ON CONFLICT(TicketId) DO UPDATE SET
                    ArchitectureReference = excluded.ArchitectureReference,
                    TechnicalAnalysis = excluded.TechnicalAnalysis,
                    OperationalRisks = excluded.OperationalRisks,
                    ResolutionSummary = excluded.ResolutionSummary,
                    ReviewedBy = excluded.ReviewedBy,
                    UpdatedDate = excluded.UpdatedDate
                """,
                new
                {
                    TicketId = ticket.Id.Value,
                    ticket.Review.ArchitectureReference,
                    ticket.Review.TechnicalAnalysis,
                    ticket.Review.OperationalRisks,
                    ticket.Review.ResolutionSummary,
                    ticket.Review.ReviewedBy,
                    UpdatedDate = ticket.Review.UpdatedDate.ToString("O")
                }, tx);
        }

        if (ticket.Escalation is not null)
        {
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Escalations WHERE TicketId = @TicketId AND EscalatedTeam = @Team",
                new { TicketId = ticket.Id.Value, Team = ticket.Escalation.EscalatedTeam }, tx);

            if (exists == 0)
            {
                await connection.ExecuteAsync("""
                    INSERT INTO Escalations (TicketId, EscalatedTeam, EscalationReason, EscalatedDate)
                    VALUES (@TicketId, @EscalatedTeam, @EscalationReason, @EscalatedDate)
                    """,
                    new
                    {
                        TicketId = ticket.Id.Value,
                        ticket.Escalation.EscalatedTeam,
                        ticket.Escalation.EscalationReason,
                        EscalatedDate = ticket.Escalation.EscalatedDate.ToString("O")
                    }, tx);
            }
        }

        foreach (var msg in ticket.Messages.Where(m => m.IsNew))
        {
            var id = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO TicketMessages (TicketId, SenderName, SenderRole, MessageText, CreatedDate)
                VALUES (@TicketId, @SenderName, @SenderRole, @MessageText, @CreatedDate);
                SELECT last_insert_rowid();
                """,
                new
                {
                    TicketId = ticket.Id.Value,
                    msg.SenderName,
                    msg.SenderRole,
                    msg.MessageText,
                    CreatedDate = msg.CreatedDate.ToString("O")
                }, tx);
            msg.MarkPersisted(id);
        }

        foreach (var note in ticket.Notes.Where(n => n.IsNew))
        {
            var id = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO TicketNotes (TicketId, Author, NoteText, CreatedDate)
                VALUES (@TicketId, @Author, @NoteText, @CreatedDate);
                SELECT last_insert_rowid();
                """,
                new
                {
                    TicketId = ticket.Id.Value,
                    note.Author,
                    note.NoteText,
                    CreatedDate = note.CreatedDate.ToString("O")
                }, tx);
            note.MarkPersisted(id);
        }

        tx.Commit();
        ticket.ClearDomainEvents();
    }

    private sealed class TicketRow
    {
        public string Id { get; set; } = default!;
        public string CreatedDate { get; set; } = default!;
        public string Priority { get; set; } = default!;
        public string RequesterName { get; set; } = default!;
        public string RequesterEmail { get; set; } = default!;
        public string Department { get; set; } = default!;
        public string ServiceStream { get; set; } = default!;
        public string Environment { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string? TechnicalContext { get; set; }
        public string Status { get; set; } = default!;
        public string? AssignedSpecialist { get; set; }
        public int TargetTatDays { get; set; }
        public string TatDueDate { get; set; } = default!;
        public string? CompletedDate { get; set; }
    }

    private sealed class TechnicalReviewRow
    {
        public string? ArchitectureReference { get; set; }
        public string? TechnicalAnalysis { get; set; }
        public string? OperationalRisks { get; set; }
        public string? ResolutionSummary { get; set; }
        public string? ReviewedBy { get; set; }
    }

    private sealed class EscalationRow
    {
        public string EscalatedTeam { get; set; } = default!;
        public string? EscalationReason { get; set; }
    }

    private sealed class MessageRow
    {
        public long Id { get; set; }
        public string SenderName { get; set; } = default!;
        public string SenderRole { get; set; } = default!;
        public string MessageText { get; set; } = default!;
        public string CreatedDate { get; set; } = default!;
    }

    private sealed class NoteRow
    {
        public long Id { get; set; }
        public string Author { get; set; } = default!;
        public string NoteText { get; set; } = default!;
        public string CreatedDate { get; set; } = default!;
    }
}
