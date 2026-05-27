using System.Data.Common;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Core.Stuff.Utils;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms.Handlers;


public record UpdateTermServiceRequest(
    long? Id,
    TermLanguage? Language,
    string? Description,
    string? Term,
    string? Changestamp) : IServiceRequest;

internal class UpdateTermHandler(
    ITermRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<UpdateTermHandler> logger) : IAppRequestHandler<UpdateTermServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(UpdateTermServiceRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        if (!request.Id.HasValue)
        {
            errors.Add("missing_id");
        }

        if (!request.Language.HasValue || !Enum.IsDefined(request.Language.Value))
        {
            errors.Add("missing_or_invalid_language");
        }

        if (string.IsNullOrWhiteSpace(request.Term))
        {
            errors.Add("missing_term");
        }

        if (string.IsNullOrWhiteSpace(request.Changestamp))
        {
            errors.Add("missing_changestamp");
        }

        if (errors.Count > 0)
        {
            return new ValidationErrors(errors);
        }

        GuardNotNull.Required(request.Id);
        GuardNotNull.Required(request.Language);
        GuardNotNull.Required(request.Term);
        GuardNotNull.Required(request.Changestamp);

        var term = StructureTermService.Invoke(request.Language.Value, request.Description ?? string.Empty, request.Term);

        if (term is not TermComputed termComputed)
        {
            return new InvalidRequestWithReason("Invalid term");
        }

        try
        {
            var newChangestamp =
                await repo.UpdateAsync(
                    request.Id.Value,
                    (int)request.Language.Value,
                    termComputed.DescriptionCleaned,
                    termComputed.TermCleaned,
                    termComputed.DenseValue,
                    termComputed.Letters,
                    termComputed.SpaceIndexes,
                    termComputed.DashIndexes,
                    termComputed.Length,
                    request.Changestamp,
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
                logger.LogError(e, "Term update failed");
            }

            return new Error(string.Empty);
        }



    }
}
