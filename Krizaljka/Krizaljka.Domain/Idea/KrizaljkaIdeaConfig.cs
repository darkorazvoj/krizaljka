
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdeaConfig(
    string Id,
    TermLanguage LanguageId,
    KrizaljkaIdeaStatus Status,
    string ThemeName,
    int TemplateRows,
    int TemplateCols,
    int TemplateZeroBlocksNum,
    int MinutesPerTemplate,
    int MaxNumOfCompletedTemplates,
    int ThemeTermsCount,
    int OtherTermsCount,
    int TemplateIdsOnlyCount,
    int TemplateIdsExcludedCount,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp);
