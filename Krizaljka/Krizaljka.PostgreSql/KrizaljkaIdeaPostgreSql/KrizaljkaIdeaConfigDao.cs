using Krizaljka.Domain.Idea;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal record KrizaljkaIdeaConfigDao(
    string Id,
    int Status,
    string ThemeName,
    int TemplateRows,
    int TemplateCols,
    int TemplateZeroBlocksNum,
    int MinutesPerTemplate,
    int MaxNumOfCompletedTemplates,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp
):IDao
{
    public TCoreModel MapTo<TCoreModel>()
    {
        if (typeof(TCoreModel) == typeof(KrizaljkaIdeaConfig))
        {
            object result = new KrizaljkaIdeaConfig(
                Id,
                (KrizaljkaIdeaStatus)Status,
                ThemeName,
                TemplateRows,
                TemplateCols,
                TemplateZeroBlocksNum,
                MinutesPerTemplate,
                MaxNumOfCompletedTemplates,
                CreatedById,
                CreatedOn,
                Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
