namespace Krizaljka.WebApi.Configs;

public class CorsConfig
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
