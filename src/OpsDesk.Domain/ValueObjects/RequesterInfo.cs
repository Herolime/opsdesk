namespace OpsDesk.Domain.ValueObjects;

public sealed record RequesterInfo
{
    public string Name { get; }
    public string Email { get; }
    public string Department { get; }

    public RequesterInfo(string name, string email, string department)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(department)) throw new ArgumentException("Department is required.", nameof(department));

        Name = name;
        Email = email;
        Department = department;
    }
}
