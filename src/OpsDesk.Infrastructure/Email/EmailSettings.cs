namespace OpsDesk.Infrastructure.Email;

public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string ActiveProfile { get; set; } = "Mock";
    public string From { get; set; } = "noreply@opsdesk.io";
    public string FromName { get; set; } = "OpsDesk";
    public Dictionary<string, SmtpProfileSettings> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mock"] = new()
        {
            DeliveryMethod = "Mock",
            Host = "localhost",
            Port = 25,
            Description = "Zero-dependency offline mock email capture"
        },
        ["Mailpit"] = new()
        {
            DeliveryMethod = "Network",
            Host = "localhost",
            Port = 1025,
            EnableSsl = false,
            Description = "Local fake SMTP server (Mailpit / Papercut)"
        },
        ["Ethereal"] = new()
        {
            DeliveryMethod = "Network",
            Host = "smtp.ethereal.email",
            Port = 587,
            EnableSsl = true,
            Description = "Ethereal Email fake SMTP testing service"
        }
    };

    public SmtpProfileSettings GetEffectiveProfile(string? overrideProfileName = null)
    {
        var target = overrideProfileName ?? ActiveProfile;
        if (!string.IsNullOrWhiteSpace(target) && Profiles.TryGetValue(target, out var profile))
            return profile;

        return Profiles["Mock"];
    }
}

public sealed class SmtpProfileSettings
{
    public string DeliveryMethod { get; set; } = "Mock";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public int TimeoutMs { get; set; } = 5000;
    public string? Description { get; set; }
}
