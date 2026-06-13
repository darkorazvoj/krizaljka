using System.Data.Common;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms.Handlers;

public record UpdateIsActiveTermServiceRequest(long? Id, bool? IsActive, string? ChangeStamp) : IServiceRequest;

internal class UpdateIsActiveTermHandler(
    ITermRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<UpdateIsActiveTermHandler> logger)
    : IAppRequestHandler<UpdateIsActiveTermServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(UpdateIsActiveTermServiceRequest request, CancellationToken ct)
    {
        if (!request.Id.HasValue)
        {
            return new InvalidRequestWithReason("Missing required values!");
        }

        if (!request.IsActive.HasValue)
        {
            return new InvalidRequestWithReason("Missing required values!");
        }

        if (string.IsNullOrWhiteSpace(request.ChangeStamp))
        {
            return new InvalidChangestamp();
        }

        try
        {
            var newChangestamp =
                await repo.UpdateIsActiveAsync(
                    request.Id.Value,
                    request.IsActive.Value,
                    request.ChangeStamp,
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
                logger.LogError(e, "IsActive Term update failed");
            }

            return new Error(string.Empty);
        }
    }
}
