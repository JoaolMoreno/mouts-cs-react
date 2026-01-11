namespace Backend.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "Backend";
    public string Audience { get; set; } = "BackendAudience";
    public string SigningKey { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 60;
    public string CookieName { get; set; } = "auth_token";
    public string RefreshCookieName { get; set; } = "refresh_token";
    public int RefreshExpiryDays { get; set; } = 7;
}

