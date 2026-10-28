namespace UTMDesk.Api.Configuration;

public sealed class SupabaseSettings
{
    public const string SectionName = "Supabase";

    public string Url { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string ServiceRoleKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = "utm-desk-evidencias";
    public int SignedUrlExpirationMinutes { get; init; } = 30;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(BucketName);

    public string ApiKey => !string.IsNullOrWhiteSpace(SecretKey)
        ? SecretKey
        : ServiceRoleKey;
}
