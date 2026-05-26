using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.Template.Services;
using Krizaljka.WebApi.Workers.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Channels;

namespace Krizaljka.WebApi.Workers;

public sealed class BatchLoadWorker(
    ChannelReader<IFileBatch> channelReader,
    IServiceScopeFactory scopeFactory,
    ILogger<BatchLoadWorker> logger) : BackgroundService
{
    

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var fileBatch in channelReader.ReadAllAsync(ct))
        {
            try
            {
                switch (fileBatch)
                {
                    case TemplateFileBatch templateFileBatch :
                        await TemplatesLoad.HandleTemplatesAsync(
                            scopeFactory,
                            templateFileBatch,
                            logger, 
                            ct);

                        break;
                    case TermFileBatch termFileBatch:
                        await TermsLoad.LoadAsync(scopeFactory, termFileBatch, logger, ct);
                        break;
                    default:
                        logger.LogWarning(
                            "Unsupported file batch type: {Type}",
                            fileBatch.GetType().FullName);
                        break;
                }
                
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process uploaded template JSON.");
            }
        }
    }
}
