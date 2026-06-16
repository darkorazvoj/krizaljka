using Krizaljka.Domain.Core.Stuff.DatabaseStuff;

namespace Krizaljka.PostgreSql;

internal class DatabaseUtils : IDatabaseUtils
{
    public string MapSqlStateToError(string? sqlState) =>
        sqlState switch
        {
            "23503" => "MissingReferencedData", //ForeignKeyViolation
            "23505" => "IdeticalRecordExits", //UniqueKeyViolation
            IDatabaseUtils.InvalidChangestampCode => "InvalidChangeStamp",
            "CC002" => "RecordNotFound",
            "CC003" => "ChangestampMissing",
            "CC004" => "UpdateValueIsEqual",
            IDatabaseUtils.ForbiddenCode => "Forbidden",
            _ => "DatabaseOperationParametersIssue"
        };
}
