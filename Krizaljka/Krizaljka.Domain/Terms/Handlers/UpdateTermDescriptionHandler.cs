using System.Data.Common;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Core.Stuff.Utils;
using Krizaljka.Domain.Extensions;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms.Handlers;


public record UpdateTermDescriptionServiceRequest(
    long? Id,
    string? Description,
    string? Changestamp) : IServiceRequest;

internal class UpdateTermDescriptionHandler(
    ITermRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<UpdateTermHandler> logger) : IAppRequestHandler<UpdateTermDescriptionServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(UpdateTermDescriptionServiceRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        if (!request.Id.HasValue)
        {
            errors.Add("missing_id");
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
        GuardNotNull.Required(request.Changestamp);
        
        try
        {
            var newChangestamp =
                await repo.UpdateDescriptionAsync(
                    request.Id.Value,
                    request.Description?.TrimExtra() ?? string.Empty,
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
