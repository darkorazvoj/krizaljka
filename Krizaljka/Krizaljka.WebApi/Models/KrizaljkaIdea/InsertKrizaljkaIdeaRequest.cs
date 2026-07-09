namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record InsertKrizaljkaIdeaRequest(
    int? LanguageId,
    string? ThemeName,
    int? TemplateRows,
    int? TemplateCols,
    int? TemplateZeroBlocksNum,
    int? MaxSolveMinutesPerTemplate,
    int? MaxNumOfCompletedTemplates
);
