using System.Data.Common;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.TermDescription.Handlers;


public record DeleteTermDescriptionServiceRequest(
    long? Id,
    string? Changestamp) : IServiceRequest;

internal class DeleteTermDescriptionHandler(
    ITermDescriptionRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<DeleteTermDescriptionHandler> logger) : IAppRequestHandler<DeleteTermDescriptionServiceRequest>
{   

    public async Task<IServiceResult> HandleAsync(DeleteTermDescriptionServiceRequest request, CancellationToken ct)
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
            await repo.DeleteAsync(
                request.Id.Value,
                request.Changestamp,
                ct);

            return new Success();
        }
        catch (DbException e)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("delete failed, database error. {sqlState}, {message}", e.SqlState, e.Message);
            }

            return dbUtils.GetSqlErrorResult(e.SqlState);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Term description delete failed");
            }

            return new Error(string.Empty);
        }

    }
}
