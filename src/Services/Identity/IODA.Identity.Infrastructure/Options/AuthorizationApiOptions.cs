namespace IODA.Identity.Infrastructure.Options;

public class AuthorizationApiOptions
{
    public const string SectionName = "AuthorizationApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ServiceApiKey { get; set; } = string.Empty;
}
