namespace Krizaljka.WebApi.Configs;

public class ConnectionStringsConfig
{
    public const string SectionName = "ConnectionStrings";

    public string Db { get; init; } = string.Empty;
}
