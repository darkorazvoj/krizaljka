
namespace Krizaljka.WebApi.Models.KrizaljkaIdea;


public record KrizaljkaIdeaConfigResponse(
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
    long TemplateIdsOnlyCount,
    int TemplateIdsExcludedCount,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp);
