using System.Xml.Linq;
using Dapper;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Krizaljka.PostgreSql.Postgres.Stuff.DataProtection;

internal sealed class DbXmlRepo(IReadOnlyDictionary<ConnStrings, string> conns)
    : BaseRepo<ConnStrings>(conns), IXmlRepository 
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var conn = GetOpenedConnection(ConnStrings.Au);
        var rows = conn.Query<string>(
            "select xml from au.DataProtectionKeys");

        return rows.Select(XElement.Parse).ToList();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var conn = GetOpenedConnection(ConnStrings.Au);
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
}
