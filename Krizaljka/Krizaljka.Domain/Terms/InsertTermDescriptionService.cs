using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms;

public class InsertTermDescriptionService(ITermRepo repo, ILogger<InsertTermDescriptionService> logger)
{
    public async Task<IServiceResult> InvokeAsync(
        long termId,
        string? description,
        long? batchId,
        long ranById,
        CancellationToken ct)
    {
        var preparedDescription = PrepareDescriptionService.Invoke(description);

        if (string.IsNullOrWhiteSpace(preparedDescription))
        {
            return new InvalidRequestWithReason("missing_description");
        }

        try
        {
            var id = await repo.InsertDescriptionAsync(
                termId,
                preparedDescription,
                batchId,
                ranById,
                DateTimeOffset.UtcNow,
                ct);

            return new SuccessInsert<long>(id);
        }
        catch (Exception e)
        {
            logger.LogError("Error inserting a term description. {Message}", e.Message);
            return new Error("InsertTermDescriptionServiceFailed");
        }
    }
}
