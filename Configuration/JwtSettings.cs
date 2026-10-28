namespace UTMDesk.Api.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "UTMDesk.Api";
    public string Audience { get; init; } = "UTMDesk.Client";
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 120;
}
