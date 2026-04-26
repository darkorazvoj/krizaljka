

namespace Krizaljka.PostgreSql.Postgres.Stuff;
public static class DaoUtils
{
    // TODO invalidate cache somehow
    private static readonly Dictionary<string, string> SelectColumnsCache = [];

    public static string GetSelectColumns(Type daoType)
    {
        if (string.IsNullOrWhiteSpace(daoType.FullName))
        {
            return GetSelectColumnsLocal();
        }

        if (SelectColumnsCache.TryGetValue(daoType.FullName, out var selectColumns))
        {
            return selectColumns;
        }

        selectColumns = GetSelectColumnsLocal();

        SelectColumnsCache.Add(daoType.FullName, selectColumns);

        return selectColumns;

        string GetSelectColumnsLocal()
        {
            selectColumns =  string.Join(',', daoType
                .GetProperties()
                .Select(p => p.Name)
                .ToList());
            return selectColumns;
        }
    }  
}
