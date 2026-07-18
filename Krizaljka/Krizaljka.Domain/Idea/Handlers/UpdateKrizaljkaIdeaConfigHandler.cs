using System.Data.Common;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Terms;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;


public record UpdateKrizaljkaIdeaConfigServiceRequest(
    string? Id, 
    int? LanguageId,
    string? ThemeName, 
    int? TemplateRows, 
    int? TemplateCols,
    int? TemplateZeroBlocksNum,
    int? MinutesPerTemplate, 
    int? MaxNumOfCompletedTemplates, 
    string? Changestamp) : IServiceRequest;

internal class UpdateKrizaljkaIdeaConfigHandler(
    IKrizaljkaIdeaRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<UpdateKrizaljkaIdeaConfigHandler> logger) : IAppRequestHandler<UpdateKrizaljkaIdeaConfigServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(UpdateKrizaljkaIdeaConfigServiceRequest request, CancellationToken ct)
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
            var newChangestamp =
                await repo.UpdateConfigAsync(
                    validParameters.Id,
                    validParameters.LanguageId,
                    validParameters.ThemeName,
                    validParameters.TemplateRows,
                    validParameters.TemplateColumns,
                    validParameters.TemplateZeroBlocksNum,
                    validParameters.MinutesPerTemplate,
                    validParameters.MaxSolvedTemplates,
                    validParameters.Changestamp,
                    ct);

            return newChangestamp is null ? new Success(): new UpdateSuccessChangestamp<string>(newChangestamp);
        }
        catch (DbException e)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Update failed, database error. {sqlState}, {message}", e.SqlState, e.Message);
            }

            return e.SqlState == IDatabaseUtils.InvalidChangestampCode
                ? new InvalidChangestamp()
                : new InvalidRequestWithReason(dbUtils.MapSqlStateToError(e.SqlState));
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Idea update failed");
            }

            return new Error(string.Empty);
        }
    }

    private static IServiceValidationResult GetValidationErrors(UpdateKrizaljkaIdeaConfigServiceRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            errors.Add("missing_id");
        }

        if (!request.LanguageId.HasValue || !Enum.IsDefined((TermLanguage)request.LanguageId.Value))
        {
            errors.Add("missing_language");
        }

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

        if (request.TemplateZeroBlocksNum is null or > KrizaljkaConstants.MaxZeroBlocks)
        {
            errors.Add("invalid_zero_blocks");
        }

        if (request.MinutesPerTemplate is null or > KrizaljkaConstants.MaxMaxSolveMinutes)
        {
            errors.Add("invalid_minutes_per_template");
        }

        if (request.MaxNumOfCompletedTemplates is null or > KrizaljkaConstants.MaxMaxCompletedTemplates)
        {
            errors.Add("invalid_max_completed_templates");
        }

        if (string.IsNullOrWhiteSpace(request.Changestamp))
        {
            errors.Add("missing_changestamp");
        }

        if (errors.Count > 0)
        {
            return new ValidationErrorsResult(errors);
        }

        return request switch
        {
            {
                Id: {} id,
                LanguageId: {} languageId,
                ThemeName: { } themeName,
                TemplateRows: { } templateRows,
                TemplateCols: { } templateCols,
                TemplateZeroBlocksNum: {} templateZeroBlocks,
                MinutesPerTemplate: {} minutesPerTemplate,
                MaxNumOfCompletedTemplates: {} maxCompleted,
                Changestamp: {} changestamp,
            } => new ValidParameters(
                id,
                languageId, 
                themeName, 
                templateRows, 
                templateCols,
                templateZeroBlocks,
                minutesPerTemplate,
                maxCompleted,
                changestamp),

            _ => new ValidationErrorsResult(["validation_failed"])
        };
    }

    private record ValidParameters(
        string Id,
        int LanguageId,
        string ThemeName,
        int TemplateRows,
        int TemplateColumns,
        int TemplateZeroBlocksNum,
        int MinutesPerTemplate,
        int MaxSolvedTemplates,
        string Changestamp) : IServiceValidationResult;
}

