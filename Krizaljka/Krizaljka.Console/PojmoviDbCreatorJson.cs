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

    private static readonly JsonSerializerOptions Options = new()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    public static async Task<(long NumberOfTerms, long NumberOfNewTerms)> CreateDatabaseAsync()
    {
        if (!Directory.Exists(DbPath))
        {
            Directory.CreateDirectory(DbPath);
        }
        var director = new DirectoryInfo(DbPath);
        var existingDbFiles = director.GetFiles($"{PojmoviDbNamePrefix}_*.json");
        var existingDbFilesFullPaths = existingDbFiles.Select(x => x.FullName)
            .ToList();

        var maxFileId = existingDbFiles
            .Select(f =>
            {
                var fileName = Path.GetFileNameWithoutExtension(f.Name);

                var parts = fileName.Split(new[] { '_', '.' });
                if (parts.Length >= 2 && int.TryParse(parts[1], out var fileId))
                {
                    return fileId;
                }

                return 0;
            })
            .DefaultIfEmpty(0)
            .Max();


        var existingDenseTerms = GetExistingDenseTerms(existingDbFilesFullPaths);

        var (validTerms, _, categories) = await new TermsLoader().LoadTermsAsync(PojmoviPath);

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
        var currentBatchId = maxFileId + 1;
        var newTerms = 0;

        foreach (var validTerm in validTerms)
        {
            if (existingDenseTerms.Contains(validTerm.DenseValue))
            {
                continue;
            }

            validTermsBatch.Add(validTerm);
            newTerms++;
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

        return (validTerms.Count, newTerms);
    }

    private static HashSet<string> GetExistingDenseTerms(List<string> existingDbFiles)
    {
        var terms =
            new HashSet<string>(StringComparer.Create(new System.Globalization.CultureInfo("hr-HR"), true));

        foreach (var dbFile in existingDbFiles)
        {
            var pojmoviDbJson = File.ReadAllText(dbFile);
            if (string.IsNullOrWhiteSpace(pojmoviDbJson))
            {
                continue;
            }

            try
            {
                var pojmoviDb = JsonSerializer.Deserialize<PojmoviJsonDb>(pojmoviDbJson, Options);
                if (pojmoviDb?.Terms is null)
                {
                    continue;
                }

                foreach (var term in pojmoviDb.Terms)
                {
                    terms.Add(term.DenseValue);
                }

            }
            catch
            {
                System.Console.WriteLine($"DB file: {dbFile} is corrupted!");
            }
        }

        return terms;
    }

    private static async Task SaveDbFileAsync(List<Term> batch, int batchId)
    {
        var pojmoviDbJsonToWrite = JsonSerializer.Serialize(new PojmoviJsonDb(batch), Options);
        await File.WriteAllTextAsync(Path.Combine(DbPath, $"{PojmoviDbNamePrefix}_{batchId:00000}.{JsonExtension}"), pojmoviDbJsonToWrite);
    
    }

}
