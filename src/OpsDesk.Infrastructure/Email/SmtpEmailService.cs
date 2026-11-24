using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsDesk.Application.Common.Interfaces;

namespace OpsDesk.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? toDisplayName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return false;

        var profile = _settings.GetEffectiveProfile();
        if (string.Equals(profile.DeliveryMethod, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "[MockEmailService] Dispatched | To: {ToEmail} ({DisplayName}) | Subject: {Subject}",
                toEmail, toDisplayName ?? "N/A", subject);
            return true;
        }

        try
        {
            using var msg = new MailMessage
            {
                From = new MailAddress(_settings.From, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(new MailAddress(toEmail, toDisplayName));

            using var client = CreateClient(profile);
            await client.SendMailAsync(msg, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP delivery failed for {ToEmail} via {Host}:{Port}", toEmail, profile.Host, profile.Port);
            return false;
        }
    }

    public async Task<(bool Success, string ProfileUsed, string Host, int Port, long ElapsedMs, string? ErrorMessage)> TestConnectionAsync(
        string toEmail,
        string? profileOverride = null,
        CancellationToken cancellationToken = default)
    {
        var profile = _settings.GetEffectiveProfile(profileOverride);
        var profileName = profileOverride ?? _settings.ActiveProfile;
        var sw = Stopwatch.StartNew();

        if (string.Equals(profile.DeliveryMethod, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            sw.Stop();
            return (true, profileName, profile.Host, profile.Port, sw.ElapsedMilliseconds, null);
        }

        try
        {
            using var msg = new MailMessage(_settings.From, toEmail, "OpsDesk SMTP Test", "<p>OpsDesk SMTP connection verified.</p>")
            {
                IsBodyHtml = true
            };
            using var client = CreateClient(profile);
            await client.SendMailAsync(msg, cancellationToken);
            sw.Stop();
            return (true, profileName, profile.Host, profile.Port, sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, profileName, profile.Host, profile.Port, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static SmtpClient CreateClient(SmtpProfileSettings profile)
    {
        var client = new SmtpClient
        {
            Host = profile.Host,
            Port = profile.Port,
            EnableSsl = profile.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = profile.TimeoutMs > 0 ? profile.TimeoutMs : 5000,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(profile.UserName) && !string.IsNullOrWhiteSpace(profile.Password))
            client.Credentials = new NetworkCredential(profile.UserName, profile.Password);

        return client;
    }
}
