using Krizaljka.Domain.Template;
using System.Threading.Channels;

namespace Krizaljka.WebApi.Workers;

public sealed class BatchLoadTemplatesWorker(
    ChannelReader<List<FileRecord>> channelReader,
    ILogger<BatchLoadTemplatesWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var files in channelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await HandleAsync(files, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process uploaded template JSON.");
            }
        }
    }

    private async Task HandleAsync(
        List<FileRecord> files,
        CancellationToken cancellationToken)
    {
        foreach (var fileRecord in files)
        {
            logger.LogInformation(message: fileRecord.Content);
        }

        // Your processing logic here.
        await Task.CompletedTask;
    }
}
