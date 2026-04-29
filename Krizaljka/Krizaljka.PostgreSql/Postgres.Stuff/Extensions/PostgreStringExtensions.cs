
namespace Krizaljka.PostgreSql.Postgres.Stuff.Extensions;
internal static class PostgreStringExtensions
{
    public static string SurroundLower(this string value) =>
        string.IsNullOrWhiteSpace(value) ? value : $"lower({value})";
}
