using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;

public record InsertKrizaljkaIdeaServiceRequest(
    string? ThemeName,
    int? TemplateRows,
    int? TemplateCols,
    int? TemplateZeroBlocksNum,
    int? MaxSolveMinutesPerTemplate,
    int? MaxNumOfCompletedTemplates) : IServiceRequest;


internal class InsertKrizaljkaIdeaHandler(
    IAuthUser authUser,
    IKrizaljkaIdeaRepo repo,
    KrizaljkaDomainOptions options,
    ILogger<InsertKrizaljkaIdeaHandler> logger) : IAppRequestHandler<InsertKrizaljkaIdeaServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(InsertKrizaljkaIdeaServiceRequest request, CancellationToken ct)
    {
        var validationResult = GetValidationErrors(request);

        if (validationResult is not ValidParameters validParameters)
        {
            return validationResult switch
            {
                ValidationErrorsResult errors => new ValidationErrors(errors.Errors),
                _ => new ValidationErrors(["validation_failed"])
            };
        }

        try
        {
            var newId = await repo.InsertAsync(
                KrizaljkaIdeaStatus.NotReady,
                validParameters.ThemeName,
                validParameters.TemplateRows,
                validParameters.TemplateColumns,
                request.TemplateZeroBlocksNum ?? 0,
                [],
                [],
                request.MaxSolveMinutesPerTemplate ?? options.MaxSolveMinutesPerTemplate,
                request.MaxNumOfCompletedTemplates ?? options.StopAfterSolvedTemplates,
                [],
                [],
                authUser.Id,
                DateTimeOffset.UtcNow,
                ct);

            return new SuccessInsert<string>(newId);

        }
        catch (Exception e)
        {
            logger.LogError("Error inserting an idea. {EMessage}", e.Message);
            return new Error("InsertKrizaljkaIdeaHandlerFailed");
        }
    }

    private static IServiceValidationResult GetValidationErrors(InsertKrizaljkaIdeaServiceRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.ThemeName))
        {
            errors.Add("missing_theme_name");
        }

        if (request.TemplateRows is null or < KrizaljkaConstants.MinTemplateRows)
        {
            errors.Add("invalid_template_rows");
        }

        if (request.TemplateCols is null or < KrizaljkaConstants.MinTemplateColumns)
        {
            errors.Add("invalid_template_cols");
        }

        if (errors.Count > 0)
        {
            return new ValidationErrorsResult(errors);
        }

        return request switch
        {
            {
                ThemeName: { } themeName,
                TemplateRows: { } templateRows,
                TemplateCols: { } templateCols
            } => new ValidParameters(themeName, templateRows, templateCols),

            _ => new ValidationErrorsResult(["validation_failed"])
        };
    }

    private record ValidParameters(string ThemeName, int TemplateRows, int TemplateColumns) : IServiceValidationResult;
}
    