
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
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp);
