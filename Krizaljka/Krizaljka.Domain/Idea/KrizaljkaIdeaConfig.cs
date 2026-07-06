
namespace Krizaljka.Domain.Idea;

public record KrizaljkaIdeaConfig(
    string Id,
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
