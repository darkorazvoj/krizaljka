using System.Data.Common;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;


public record RemoveIdKrizaljkaIdeaServiceRequest(
    string? Id, 
    string? ColumnName, 
    long? ItemId, 
    string? Changestamp) : IServiceRequest;

internal class RemoveIdKrizaljkaIdeaHandler(
    IKrizaljkaIdeaRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<RemoveIdKrizaljkaIdeaHandler> logger) : IAppRequestHandler<RemoveIdKrizaljkaIdeaServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(RemoveIdKrizaljkaIdeaServiceRequest request, CancellationToken ct)
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
                await repo.RemoveIdAsync(
                    validParameters.Id,
                    validParameters.ColumnName,
                    validParameters.NewId,
                    validParameters.Changestamp,
                    ct);

            return newChangestamp is null ? new Success(): new UpdateSuccessChangestamp<string>(newChangestamp);
        }
        catch (DbException e)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Idea ID remove failed, database error. {sqlState}, {message}", e.SqlState, e.Message);
            }

            return e.SqlState == IDatabaseUtils.InvalidChangestampCode
                ? new InvalidChangestamp()
                : new InvalidRequestWithReason(dbUtils.MapSqlStateToError(e.SqlState));
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Idea ID remove failed");
            }

            return new Error(string.Empty);
        }
    }

    private static IServiceValidationResult GetValidationErrors(RemoveIdKrizaljkaIdeaServiceRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            errors.Add("missing_id");
        }

        if (string.IsNullOrWhiteSpace(request.ColumnName))
        {
            errors.Add("missing_column_name");
        }

        if (!request.ItemId.HasValue)
        {
            errors.Add("missing_item_id");
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
                ColumnName: {} columnName,
                ItemId: { } itemId,
                Changestamp: {} changestamp,
            } => new ValidParameters(
                id,
                columnName, 
                itemId, 
                changestamp),

            _ => new ValidationErrorsResult(["validation_failed"])
        };
    }

    private record ValidParameters(
        string Id,
        string ColumnName,
        long NewId,
        string Changestamp) : IServiceValidationResult;
}

