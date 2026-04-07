using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Console;

public static class PojmoviDbCreatorJson
{
    private const string PojmoviPath = @"C:\git\krizaljka\pojmovi";
    private const string DbPath = @"C:\git\krizaljka\pojmovi\db";
    private const string CategoriesDbName = "kategorije.json";
    private const string PojmoviDbNamePrefix = "pojmovi";
    private const string JsonExtension = ".json";
    private const long MaxTermsPerFile = 100000;

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    public static async Task<long> CreateDatabaseAsync()
    {
        var (validTerms, _, categories) = await new TermsLoader().LoadTermsAsync(PojmoviPath);

        if (!Directory.Exists(DbPath))
        {
            Directory.CreateDirectory(DbPath);
        }

        List<CategoryJsonDbItem> categoryItems = [];
        foreach (var kv in categories)
        {
            categoryItems.Add(new CategoryJsonDbItem(kv.Key, kv.Value));
        }

        var categoriesFullPath = Path.Combine(DbPath, CategoriesDbName);

        if (File.Exists(categoriesFullPath))
        {
            File.Delete(categoriesFullPath);
        }

        var categoryDb = new CategoryJsonDb(categoryItems);
        var categoriesDbJson = JsonSerializer.Serialize(categoryDb, Options);
        await File.WriteAllTextAsync(categoriesFullPath, categoriesDbJson);

        List<Term> validTermsBatch = [];
        var currentBatchSize = 0;
        var currentBatchId = 1;

        foreach (var validTerm in validTerms)
        {
            validTermsBatch.Add(validTerm);
            currentBatchSize++;

            if (currentBatchSize >= MaxTermsPerFile)
            {
                await SaveDbFileAsync(validTermsBatch, currentBatchId);
                currentBatchId++;
                currentBatchSize = 0;
                validTermsBatch.Clear();
            }
        }


        if (validTermsBatch.Count > 0)
        {
            await SaveDbFileAsync(validTermsBatch, currentBatchId);
        }

        return validTerms.Count;
    }

    private static async Task SaveDbFileAsync(List<Term> batch, int batchId)
    {
        var pojmoviDbJsonToWrite = JsonSerializer.Serialize(new PojmoviJsonDb(batch), Options);
        await File.WriteAllTextAsync(Path.Combine(DbPath, $"{PojmoviDbNamePrefix}_{batchId:00000}.{JsonExtension}"), pojmoviDbJsonToWrite);
    
    }

}
