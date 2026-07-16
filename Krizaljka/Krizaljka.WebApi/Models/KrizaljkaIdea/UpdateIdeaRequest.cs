namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record UpdateIdeaRequest(
    int? LanguageId,
    string? ThemeName,
    int? TemplateRows,
    int? TemplateCols,
    int? TemplateZeroBlocksNum,
    int? MinutesPerTemplate,
    int? MaxNumOfCompletedTemplates,
    string? Changestamp);
