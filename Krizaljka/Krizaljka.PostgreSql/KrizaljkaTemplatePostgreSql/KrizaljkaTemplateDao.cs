
using Krizaljka.Domain.Template;

namespace Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;

internal record KrizaljkaTemplateDao(
    long Id,
    string? Name,
    int[][] Matrix,
    int NumRows,
    int NumColumns,
    bool IsActive,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp)
{
    public KrizaljkaTemplate MapTo() =>
        new(
            Id,
            Name,
            Matrix,
            NumRows,
            NumColumns,
            IsActive,
            CreatedById,
            CreatedOn,
            Changestamp);
}
