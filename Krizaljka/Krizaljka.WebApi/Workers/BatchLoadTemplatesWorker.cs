using Krizaljka.Domain.Template;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Channels;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Template.Services;

namespace Krizaljka.WebApi.Workers;

public sealed class BatchLoadTemplatesWorker(
    ChannelReader<List<FileRecord>> channelReader,
    IServiceScopeFactory scopeFactory,
    ILogger<BatchLoadTemplatesWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Options = new()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), PropertyNameCaseInsensitive = true  };

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
        var list = GetTemplates(files);

        if (list.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var insertTemplateService = scope.ServiceProvider.GetRequiredService<InsertTemplateService>();


        foreach (var template in list)
        {
            // TODO - SERVICE USER ID
            var insertKrizaljkaResult =
                await insertTemplateService.InvokeAsync(template.Rows, template.Name, 7, cancellationToken);

            if (insertKrizaljkaResult is not SuccessInsert<long>)
            {
                logger.LogWarning(message: "Template not saved {name}",
                    string.IsNullOrWhiteSpace(template.Name) ? "<no name>" : template.Name);
            }
        }


        await Task.CompletedTask;
    }

    private List<KrizaljkaTemplateJson> GetTemplates(List<FileRecord> files)
    {   
        List<KrizaljkaTemplateJson> templates = [];

        foreach (var fileRecord in files)
        {
            var one = TryDeserializeOne(fileRecord.Content);

            if (one is not null)
            {
                templates.Add(one);
                continue;
            }

            var list = TryDeserializeList(fileRecord.Content);
            templates.AddRange(list);

        }

        return templates;
    }

    private KrizaljkaTemplateJson? TryDeserializeOne(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                logger.LogInformation(message:"Invalid JSON object {jsonStringTrimmed}", string.Empty);
                return null;
            }

            var one = JsonSerializer.Deserialize<KrizaljkaTemplateJson>(json, Options);
            if (one?.Rows is null)
            {
                logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                    json.Substring(0, Math.Min(json.Length, 100)));
                return null;
            }

            return one;

        }
        catch
        {
            logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                json.Substring(0, Math.Min(json.Length, 100)));
            return null;
        }
    }

    private List<KrizaljkaTemplateJson> TryDeserializeList(string json)
    {
        List<KrizaljkaTemplateJson> templates = [];

        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                logger.LogInformation(message:"Invalid JSON object {jsonStringTrimmed}", string.Empty);
                return [];
            }

            var list = JsonSerializer.Deserialize<TemplatesJson?>(json, Options);
            if (list is null)
            {
                logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                    json.Substring(0, Math.Min(json.Length, 200)));
                return [];
            }

            foreach (var template in list.Templates)
            {
                if (template.Rows is null)
                {
                    continue;
                }

                templates.Add(template);
            }

            return templates;

        }
        catch
        {
            logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                json.Substring(0, Math.Min(json.Length, 200)));
            return [];
        }
    }

    private record TemplatesJson(List<KrizaljkaTemplateJson> Templates);
}
