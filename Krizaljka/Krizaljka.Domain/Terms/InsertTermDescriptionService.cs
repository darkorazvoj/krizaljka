using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Krizaljka.Domain.Terms;

public class InsertTermDescriptionService(
    ITermRepo repo, 
    IDatabaseUtils dbUtils,
    ILogger<InsertTermDescriptionService> logger)
{
    public async Task<IServiceResult> InvokeAsync(
        long? termId,
        string? description,
        long? batchId,
        long ranById,
        CancellationToken ct)
    {
        if (!termId.HasValue)
        {
            return new InvalidRequestWithReason("missing_termId");
        }

        var preparedDescription = PrepareDescriptionService.Invoke(description);

        if (string.IsNullOrWhiteSpace(preparedDescription))
        {
            return new InvalidRequestWithReason("missing_description");
        }

        try
        {
            var id = await repo.InsertDescriptionAsync(
                termId.Value,
                preparedDescription,
                batchId,
                ranById,
                DateTimeOffset.UtcNow,
                ct);

            return new SuccessInsert<long>(id);
        }
        catch (DbException e)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Database error. {sqlState}, {message}", e.SqlState, e.Message);
            }

            return dbUtils.GetSqlErrorResult(e.SqlState);
        }
        catch (Exception e)
        {
            logger.LogError("Error inserting a term description. {Message}", e.Message);
            return new Error("InsertTermDescriptionServiceFailed");
        }
    }
}
