using System.Globalization;
using Dapper;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Entities;
using OpsDesk.Application.Enums;
using OpsDesk.Infrastructure.Persistence.Db;

namespace OpsDesk.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository, ITicketNumberGenerator
{
    private readonly IDbConnectionFactory _factory;

    public TicketRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<string> NextAsync(CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        using var tx = connection.BeginTransaction();
        var next = await connection.ExecuteScalarAsync<int>(
            "UPDATE Sequence SET LastValue = LastValue + 1 WHERE Id = 1 RETURNING LastValue;",
            transaction: tx);
        tx.Commit();
        return $"OPS-{next:D4}";
    }

    public async Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default)
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

        var ticket = Ticket.Create(
            row.Id,
            row.RequesterName,
            row.RequesterEmail,
            row.Department,
            Enum.Parse<Priority>(row.Priority, ignoreCase: true),
            Enum.Parse<ServiceStream>(row.ServiceStream, ignoreCase: true),
            Enum.Parse<DeploymentEnvironment>(row.Environment, ignoreCase: true),
            row.Category,
            row.Title,
            row.Description,
            row.TargetTatDays,
            DateTime.Parse(row.TatDueDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            row.TechnicalContext);

        ticket.Hydrate(
            DateTime.Parse(row.CreatedDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Enum.Parse<TicketStatus>(row.Status, ignoreCase: true),
            row.AssignedSpecialist,
            row.ReturnReason,
            row.ReturnedBy,
            row.ReturnedAt is null ? null : DateTime.Parse(row.ReturnedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime.Parse(row.TatDueDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            row.TargetTatDays,
            row.CompletedDate is null ? null : DateTime.Parse(row.CompletedDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            row.ClosedBy,
            review,
            escalation,
            messages,
            notes);

        return ticket;
    }

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync("""
            INSERT INTO Tickets (
                Id, CreatedDate, Priority, RequesterName, RequesterEmail, Department,
                ServiceStream, Environment, Category, Title, Description, TechnicalContext,
                Status, AssignedSpecialist, ReturnReason, ReturnedBy, ReturnedAt,
                TargetTatDays, TatDueDate, CompletedDate, ClosedBy
            ) VALUES (
                @Id, @CreatedDate, @Priority, @RequesterName, @RequesterEmail, @Department,
                @ServiceStream, @Environment, @Category, @Title, @Description, @TechnicalContext,
                @Status, @AssignedSpecialist, @ReturnReason, @ReturnedBy, @ReturnedAt,
                @TargetTatDays, @TatDueDate, @CompletedDate, @ClosedBy
            )
            """,
            new
            {
                ticket.Id,
                CreatedDate = ticket.CreatedDate.ToString("O"),
                Priority = ticket.Priority.ToString(),
                ticket.RequesterName,
                ticket.RequesterEmail,
                ticket.Department,
                ServiceStream = ticket.ServiceStream.ToString(),
                Environment = ticket.Environment.ToString(),
                ticket.Category,
                ticket.Title,
                ticket.Description,
                ticket.TechnicalContext,
                Status = ticket.Status.ToString(),
                ticket.AssignedSpecialist,
                ticket.ReturnReason,
                ticket.ReturnedBy,
                ReturnedAt = ticket.ReturnedAt?.ToString("O"),
                ticket.TargetTatDays,
                TatDueDate = ticket.TatDueDate.ToString("O"),
                CompletedDate = ticket.CompletedDate?.ToString("O"),
                ticket.ClosedBy
            }, tx);

        tx.Commit();
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync("""
            UPDATE Tickets SET
                Title = @Title,
                Description = @Description,
                TechnicalContext = @TechnicalContext,
                Priority = @Priority,
                Status = @Status,
                AssignedSpecialist = @AssignedSpecialist,
                ReturnReason = @ReturnReason,
                ReturnedBy = @ReturnedBy,
                ReturnedAt = @ReturnedAt,
                TargetTatDays = @TargetTatDays,
                TatDueDate = @TatDueDate,
                CompletedDate = @CompletedDate,
                ClosedBy = @ClosedBy
            WHERE Id = @Id
            """,
            new
            {
                ticket.Id,
                ticket.Title,
                ticket.Description,
                ticket.TechnicalContext,
                Priority = ticket.Priority.ToString(),
                Status = ticket.Status.ToString(),
                ticket.AssignedSpecialist,
                ticket.ReturnReason,
                ticket.ReturnedBy,
                ReturnedAt = ticket.ReturnedAt?.ToString("O"),
                ticket.TargetTatDays,
                TatDueDate = ticket.TatDueDate.ToString("O"),
                CompletedDate = ticket.CompletedDate?.ToString("O"),
                ticket.ClosedBy
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
                    TicketId = ticket.Id,
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
                new { TicketId = ticket.Id, Team = ticket.Escalation.EscalatedTeam }, tx);

            if (exists == 0)
            {
                await connection.ExecuteAsync("""
                    INSERT INTO Escalations (TicketId, EscalatedTeam, EscalationReason, EscalatedDate)
                    VALUES (@TicketId, @EscalatedTeam, @EscalationReason, @EscalatedDate)
                    """,
                    new
                    {
                        TicketId = ticket.Id,
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
                    TicketId = ticket.Id,
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
                    TicketId = ticket.Id,
                    note.Author,
                    note.NoteText,
                    CreatedDate = note.CreatedDate.ToString("O")
                }, tx);
            note.MarkPersisted(id);
        }

        tx.Commit();
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
        public string? ReturnReason { get; set; }
        public string? ReturnedBy { get; set; }
        public string? ReturnedAt { get; set; }
        public int TargetTatDays { get; set; }
        public string TatDueDate { get; set; } = default!;
        public string? CompletedDate { get; set; }
        public string? ClosedBy { get; set; }
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
