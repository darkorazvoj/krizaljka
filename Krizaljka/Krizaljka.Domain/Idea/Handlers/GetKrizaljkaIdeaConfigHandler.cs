using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;


public record GetKrizaljkaIdeaConfigServiceRequest(string Id) : IServiceRequest;


internal class GetKrizaljkaIdeaConfigHandler(
    IKrizaljkaIdeaRepo repo,
    ILogger<GetKrizaljkaIdeaConfigHandler> logger)
    : IAppRequestHandler<GetKrizaljkaIdeaConfigServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetKrizaljkaIdeaConfigServiceRequest request, CancellationToken ct)
    {
        try
        {
            var template = await repo.GetConfigAsync(request.Id, ct);

            return template is null ? new NoData() : new Success<KrizaljkaIdeaConfig>(template);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get krizaljka idea failed");
            }

            return new Error(string.Empty);
        }
    }
}
