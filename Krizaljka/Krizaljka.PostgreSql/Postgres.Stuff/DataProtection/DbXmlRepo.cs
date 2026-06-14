using System.Xml.Linq;
using Dapper;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Npgsql;

namespace Krizaljka.PostgreSql.Postgres.Stuff.DataProtection;

internal sealed class DbXmlRepo(ConnectionStrings connectionStrings) :IXmlRepository 
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var conn = GetOpenedConnection(connectionStrings.GetConnectionString(ConnStrings.Au));
        var rows = conn.Query<string>(
            "select xml from au.DataProtectionKeys");

        return rows.Select(XElement.Parse).ToList();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var conn = GetOpenedConnection(connectionStrings.GetConnectionString(ConnStrings.Au));
        conn.Execute(
            """
                        insert into au.dataprotectionkeys (friendlyName, xml)
                        values (@name, @xml);
            """,
            new
            {
                name = friendlyName,
                xml = element.ToString(SaveOptions.DisableFormatting)
            });
    }

    private static NpgsqlConnection GetOpenedConnection(string connectionString)
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }
}
