using Krizaljka.Domain.Terms;
using Krizaljka.WebApi.Workers.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.WebApi.Workers;

internal static class TermsLoad
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), PropertyNameCaseInsensitive = true
    };

    public static async Task LoadAsync(
        IServiceScopeFactory scopeFactory,
        TermFileBatch fileBatch,
        ILogger logger,
        CancellationToken ct)
    {
        var list = GetTemplates(fileBatch, logger);

        if (list.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var insertTermService = scope.ServiceProvider.GetRequiredService<InsertTermService>();

        foreach (var termJson in list)
        {
            // TODO - SERVICE USER ID
            var insertResult =
                await insertTermService.InvokeAsync(
                    fileBatch.Language,
                    termJson.Description,
                    termJson.Term,
                    false,
                    7,
                    ct);

            if (insertResult is not SuccessInsert<long>)
            {
                logger.LogWarning(message: "Term not saved {term}",
                    string.IsNullOrWhiteSpace(termJson.Term) ? "<empty>" : termJson.Term);
            }
        }
    }

    private static List<TermJson> GetTemplates(TermFileBatch fileBatch, ILogger logger)
    {
        List<TermJson> terms = [];

        foreach (var fileRecord in fileBatch.Contents)
        {
            var one = TryDeserializeOne(fileRecord.Content, logger);

            if (one is not null)
            {
                terms.Add(one);
                continue;
            }

            var list = TryDeserializeList(fileRecord.Content, logger);
            terms.AddRange(list);

        }

        return terms;
    }

    private static TermJson? TryDeserializeOne(string json, ILogger logger)
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

            var one = JsonSerializer.Deserialize<TermJson>(json, Options);
            if (one?.Term is null)
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

    private static List<TermJson> TryDeserializeList(string json, ILogger logger)
    {
        List<TermJson> termJsonList = [];

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

            var list = JsonSerializer.Deserialize<List<TermJson>?>(json, Options);
            if (list is null || list.Count == 0)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(message: "Invalid JSON object {jsonStringTrimmed}",
                        json[..Math.Min(json.Length, 200)]);
                }

                return [];
            }

            foreach (var termJson in list)
            {

                termJsonList.Add(termJson);
            }

            return termJsonList;

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
