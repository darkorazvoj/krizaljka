using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Template.Handlers;

public record GetKrizaljkaTemplatesForExportServiceRequest(List<long> Ids) : IServiceRequest;

internal class GetKrizaljkaTemplatesForExportHandler(
    IKrizaljkaTemplateRepo repo,
    ILogger<GetKrizaljkaTemplatesForExportHandler> logger)
    : IAppRequestHandler<GetKrizaljkaTemplatesForExportServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetKrizaljkaTemplatesForExportServiceRequest request, CancellationToken ct)
    {
        try
        {
            var template = await repo.GetForExportAsync(request.Ids, ct);

            return new Success<List<KrizaljkaTemplateExport>>(template);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get List of Krizaljka templates failed");
            }

            return new Error(string.Empty);
        }
    }
}
