using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Template.Handlers;

public record GetKrizaljkaTemplateServiceRequest(long Id) : IServiceRequest;

internal class GetKrizaljkaTemplateHandler(
    IKrizaljkaTemplateRepo repo,
    ILogger<GetKrizaljkaTemplateHandler> logger)
    : IAppRequestHandler<GetKrizaljkaTemplateServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetKrizaljkaTemplateServiceRequest request, CancellationToken ct)
    {
        try
        {
            var template = await repo.GetAsync(request.Id, ct);

            return template is null ? new NoData() : new Success<KrizaljkaTemplate>(template);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get Krizaljka template failed");
            }

            return new Error(string.Empty);
        }
    }
}
