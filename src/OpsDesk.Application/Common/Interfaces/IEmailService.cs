namespace OpsDesk.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? toDisplayName = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string ProfileUsed, string Host, int Port, long ElapsedMs, string? ErrorMessage)> TestConnectionAsync(
        string toEmail,
        string? profileOverride = null,
        CancellationToken cancellationToken = default);
}
