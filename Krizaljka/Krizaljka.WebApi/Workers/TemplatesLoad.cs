using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.Template.Services;
using Krizaljka.WebApi.Workers.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Krizaljka.WebApi.Workers;

internal static class TemplatesLoad
{
    private static readonly JsonSerializerOptions Options = new()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), PropertyNameCaseInsensitive = true  };

    public record TemplatesJson(List<KrizaljkaTemplateJson> Templates);

    public static async Task HandleTemplatesAsync(
        IServiceScopeFactory scopeFactory,
        TemplateFileBatch fileBatch,
        ILogger logger,
        CancellationToken ct)
    {
        var list = GetTemplates(fileBatch, logger);

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
                await insertTemplateService.InvokeAsync(template.Rows, template.Name, 7, ct);

            if (insertKrizaljkaResult is not SuccessInsert<long>)
            {
                logger.LogWarning(message: "Template not saved {name}",
                    string.IsNullOrWhiteSpace(template.Name) ? "<no name>" : template.Name);
            }
        }


        await Task.CompletedTask;
    }

    private static List<KrizaljkaTemplateJson> GetTemplates(TemplateFileBatch fileBatch, ILogger logger)
    {   
        List<KrizaljkaTemplateJson> templates = [];

        foreach (var fileRecord in fileBatch.Contents)
        {
            var one = TryDeserializeOne(fileRecord.Content, logger);

            if (one is not null)
            {
                templates.Add(one);
                continue;
            }

            var list = TryDeserializeList(fileRecord.Content, logger);
            templates.AddRange(list);

        }

        return templates;
    }

      private static KrizaljkaTemplateJson? TryDeserializeOne(string json, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}", string.Empty);
                }

                return null;
            }

            var one = JsonSerializer.Deserialize<KrizaljkaTemplateJson>(json, Options);
            if (one?.Rows is null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                        json[..Math.Min(json.Length, 100)]);
                }

                return null;
            }

            return one;

        }
        catch
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                    json[..Math.Min(json.Length, 100)]);
            }

            return null;
        }
    }

    private static List<KrizaljkaTemplateJson> TryDeserializeList(string json, ILogger logger)
    {
        List<KrizaljkaTemplateJson> templates = [];

        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}", string.Empty);
                }

                return [];
            }

            var list = JsonSerializer.Deserialize<TemplatesJson?>(json, Options);
            if (list is null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                        json[..Math.Min(json.Length, 200)]);
                }

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
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                    json[..Math.Min(json.Length, 200)]);
            }

            return [];
        }
    }
}
