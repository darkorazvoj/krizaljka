using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;

namespace Krizaljka.Domain.Template.Handlers;

public record UpdateIsActiveKrizaljkaTemplateServiceRequest(long? Id, bool? IsActive, string? ChangeStamp) : IServiceRequest;

internal class UpdateIsActiveTermHandler(
    IKrizaljkaTemplateRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<UpdateIsActiveTermHandler> logger)
    : IAppRequestHandler<UpdateIsActiveKrizaljkaTemplateServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(UpdateIsActiveKrizaljkaTemplateServiceRequest request, CancellationToken ct)
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

            return dbUtils.GetSqlErrorResult(e.SqlState);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "IsActive Krizaljka Template failed");
            }

            return new Error(string.Empty);
        }
    }
}
