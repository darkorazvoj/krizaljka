using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms.Handlers;

public record GetTermsForExportServiceRequest(TermLanguage LanguageId) : IServiceRequest;

internal class GetTermsForExportHandler(
    ITermRepo repo,
    ILogger<GetTermsForExportHandler> logger)
    : IAppRequestHandler<GetTermsForExportServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetTermsForExportServiceRequest request, CancellationToken ct)
    {
        try
        {
            var list = await repo.GetForExportAsync((int)request.LanguageId, ct);

            return new Success<List<TermExport>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get list of terms for export failed");
            }

            return new Error(string.Empty);
        }
    }
}
