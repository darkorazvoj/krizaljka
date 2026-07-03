using Krizaljka.Domain.Idea;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal record KrizaljkaIdeaDao(
    string Id,
    int Status,
    string ThemeName,
    int TemplateRows,
    int TemplateCols,
    int TemplateZeroBlocksNum,
    List<long> ThemeTerms,
    List<long> OtherTerms,
    int MinutesPerTemplate,
    int MaxNumOfCompletedTemplates,
    List<long> TemplateIdsOnly,
    List<long> TemplateIdsExcluded,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp
):IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaIdea))
        {
            object result = new KrizaljkaIdea(
                Id,
                (KrizaljkaIdeaStatus)Status,
                ThemeName,
                TemplateRows,
                TemplateCols,
                TemplateZeroBlocksNum,
                ThemeTerms,
                OtherTerms,
                MinutesPerTemplate,
                MaxNumOfCompletedTemplates,
                TemplateIdsOnly,
                TemplateIdsExcluded,
                CreatedById,
                CreatedOn,
                Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
