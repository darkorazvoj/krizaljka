
namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdea(
    string Id,
    KrizaljkaIdeaStatus Status,
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
    string Changestamp);
