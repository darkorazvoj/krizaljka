namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record InsertKrizaljkaIdeaRequest(
    string? ThemeName,
    int? TemplateRows,
    int? TemplateCols,
    int? TemplateZeroBlocksNum,
    int? MaxSolveMinutesPerTemplate,
    int? MaxNumOfCompletedTemplates
);
