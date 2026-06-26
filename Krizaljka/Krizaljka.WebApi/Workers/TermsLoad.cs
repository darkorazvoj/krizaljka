using Krizaljka.Domain.Terms;
using Krizaljka.WebApi.Workers.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.TermDescription;

namespace Krizaljka.WebApi.Workers;

internal static class TermsLoad
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, 
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), 
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };

    private sealed class Counters
    {
        public int NumberOfTerms { get; init; }
        public int InsertedTerms { get; set; }
        public int NumberOfDescriptions { get; set; }
        public int InsertedDescriptions { get; set; }
    }

    public static async Task LoadAsync(
        IServiceScopeFactory scopeFactory,
        TermFileBatch fileBatch,
        ILogger logger,
        CancellationToken ct)
    {
        var list = GetTerms(fileBatch, logger);

        if (list.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var batchRepo = scope.ServiceProvider.GetRequiredService<ITermImportBatchRepo>();
        var insertTermService = scope.ServiceProvider.GetRequiredService<InsertTermService>();
        var insertTermDescriptionService = scope.ServiceProvider.GetRequiredService<InsertTermDescriptionService>();
        
        var batchId = await batchRepo.InsertAsync(fileBatch.RanById, DateTimeOffset.UtcNow, ct);

        Counters counters = new()
        {
            NumberOfTerms = list.Count
        };

        foreach (var termBatchImport in list)
        {
            var oneDescription = termBatchImport.Descriptions.Count == 1 ? termBatchImport.Descriptions[0] : null;

            var insertResult =
                await insertTermService.InvokeAsync(
                    fileBatch.Language,
                    oneDescription,
                    termBatchImport.Term,
                    false,
                    batchId,
                    fileBatch.RanById,
                    ct);

            if (insertResult is SuccessInsert<long> successTermInsert)
            {
                counters.InsertedTerms++;
                if (oneDescription is not null)
                {
                    counters.NumberOfDescriptions++;
                    counters.InsertedDescriptions++;
                }
                else if (termBatchImport.Descriptions.Count > 1)
                {
                    await InsertDescriptionsAsync(successTermInsert.Id, termBatchImport.Descriptions);
                }
            }
            else if (insertResult is RecordExists recordExists)
            {
                var existingId =  Convert.ToInt64(recordExists.ExistingId);
                if (existingId > 0 && termBatchImport.Descriptions.Count > 0)
                {
                    await InsertDescriptionsAsync(existingId, termBatchImport.Descriptions);
                }
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Number of Terms: {numOfTerms}, Inserted Terms: {insertedTerms}, Number of descriptions: {numOfDescriptions}, Inserted Descriptions: {insertedDescriptions}",
                counters.NumberOfTerms, counters.InsertedTerms, counters.NumberOfDescriptions,
                counters.InsertedDescriptions);
        }

        return;

        async Task InsertDescriptionsAsync(long id, List<string> descriptions)
        {
            foreach (var description in descriptions)
            {
                counters.NumberOfDescriptions++;

                var insertDescriptionResult = await insertTermDescriptionService.InvokeAsync(
                    id,
                    description,
                    batchId,
                    fileBatch.RanById,
                    ct);

                if (insertDescriptionResult is SuccessInsert<long>)
                {
                    counters.InsertedDescriptions++;
                }
            }
        }
    }

    private static List<TermBatchImport> GetTerms(TermFileBatch fileBatch, ILogger logger)
    {
        List<TermBatchImport> terms = [];

        foreach (var fileRecord in fileBatch.Contents)
        {
            var one = TryDeserializeOne(fileRecord.Content, logger);

            if (one is not null)
            {
                terms.Add(one);
                continue;
            }

            var list = TryDeserializeList(fileRecord.Content, logger);
            if (list.Count > 0)
            {
                terms.AddRange(list);
                continue;
            }

            var listMultipleDescriptions = TryDeserializeMultipleDescriptionsList(fileRecord.Content, logger);
            if (listMultipleDescriptions.Count > 0)
            {
                terms.AddRange(listMultipleDescriptions);
            }

        }

        return terms;
    }

    private static TermBatchImport? TryDeserializeOne(string json, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(message: "It's not one TermsJson because string is null or empty {jsonStringTrimmed}", string.Empty);
                }

                return null;
            }

            var one = JsonSerializer.Deserialize<TermJson>(json, Options);
            if (one?.Term is null)
            {
                return null;
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(message: "JSON file is one object");
            }

            return new TermBatchImport(one.Term, [one.Description]);

        }
        catch
        {
            return null;
        }
    }

    private static List<TermBatchImport> TryDeserializeList(string json, ILogger logger)
    {
        List<TermBatchImport> termJsonList = [];

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
                return [];
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(message: "JSON file is 'one description record'");
            }

            foreach (var termJson in list)
            {

                termJsonList.Add(new TermBatchImport(termJson.Term, [termJson.Description]));
            }

            return termJsonList;

        }
        catch
        {
            return [];
        }
    }

    private static List<TermBatchImport> TryDeserializeMultipleDescriptionsList(string json, ILogger logger)
    {
        List<TermBatchImport> termJsonList = [];

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

            var list = JsonSerializer.Deserialize<List<TermMultipleDescriptionsJson>?>(json, Options);
            if (list is null || list.Count == 0)
            {
                return [];
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(message: "JSON file is 'one description record'");
            }

            foreach (var termJson in list)
            {

                termJsonList.Add(new TermBatchImport(termJson.Term, termJson.Description));
            }

            return termJsonList;

        }
        catch
        {
            return [];
        }
    }

    private record TermBatchImport(string Term, List<string> Descriptions);
}


