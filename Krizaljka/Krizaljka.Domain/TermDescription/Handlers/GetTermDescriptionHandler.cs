using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.TermDescription.Handlers;

public record GetTermDescriptionServiceRequest(long Id) : IServiceRequest;


internal class GetTermDescriptionHandler(
    ITermDescriptionRepo repo,
    ILogger<GetTermDescriptionHandler> logger)
    : IAppRequestHandler<GetTermDescriptionServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetTermDescriptionServiceRequest request, CancellationToken ct)
    {
        try
        {
            var obj = await repo.GetAsync(request.Id, ct);

            return obj is null ? new NoData() : new Success<TermDescription>(obj);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get {objName} failed", nameof(TermDescription));
            }

            return new Error(string.Empty);
        }
    }
}
