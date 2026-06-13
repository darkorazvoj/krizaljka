namespace Krizaljka.PostgreSql.Postgres.Stuff;

public class ConnectionStrings(IReadOnlyDictionary<ConnStrings, string> connectionStrings)
{
    public string GetConnectionString(ConnStrings connKey)
    {
        if (!connectionStrings.TryGetValue(connKey, out var connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string registered for connKey '{connKey}'");
        }

        return connectionString;
    }
}
