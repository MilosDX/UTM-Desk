namespace UTMDesk.Api.Configuration;

public sealed class SuperAdminSeedSettings
{
    public const string SectionName = "SuperAdminSeed";

    public bool Enabled { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
