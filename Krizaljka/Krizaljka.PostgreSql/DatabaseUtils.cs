using Krizaljka.Domain.Core.Stuff.Utils;

namespace Krizaljka.PostgreSql;

internal class DatabaseUtils : IDatabaseUtils
{
    public string MapSqlStateToError(string? sqlState) =>
        sqlState switch
        {
            "23503" => "ForeignKeyViolation",
            "23505" => "UniqueKeyViolation",
            IDatabaseUtils.InvalidChangestampCode => "InvalidChangeStamp",
            "CC002" => "RecordNotFound",
            "CC003" => "ChangestampMissing",
            "CC004" => "UpdateValueIsEqual",
            "CC005" => "Forbidden",
            _ => "DatabaseOperationParametersIssue"
        };
}
