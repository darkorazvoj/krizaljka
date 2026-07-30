using Krizaljka.Domain.Idea;
using Krizaljka.Domain.Terms;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal record KrizaljkaIdeaConfigDao(
    string Id,
    int LanguageId,
    int Status,
    string ThemeName,
    int TemplateRows,
    int TemplateCols,
    int TemplateZeroBlocksNum,
    int MinutesPerTemplate,
    int MaxNumOfCompletedTemplates,
    int ThemeTermsCount,
    int OtherTermsCount,
    long TemplateIdsCount,
    bool IsTemplatesOnly,
    int TemplateIdsExcludedCount,
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
                (TermLanguage)LanguageId,
                (KrizaljkaIdeaStatus)Status,
                ThemeName,
                TemplateRows,
                TemplateCols,
                TemplateZeroBlocksNum,
                MinutesPerTemplate,
                MaxNumOfCompletedTemplates,
                ThemeTermsCount,
                OtherTermsCount,
                TemplateIdsCount,
                IsTemplatesOnly,
                TemplateIdsExcludedCount,
                CreatedById,
                CreatedOn,
                Changestamp);
            return (TCoreModel)result;
        }

        throw new InvalidOperationException($"Unsupported mapping to {typeof(TCoreModel).Name}");
    }
}
