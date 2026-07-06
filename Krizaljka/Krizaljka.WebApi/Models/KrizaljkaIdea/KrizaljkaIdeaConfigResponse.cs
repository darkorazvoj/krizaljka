
namespace Krizaljka.WebApi.Models.KrizaljkaIdea;


public record KrizaljkaIdeaConfigResponse(
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
    string Changestamp);
