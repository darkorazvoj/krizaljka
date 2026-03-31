using System.Text.Json;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Console;

public class TermsLoader
{
    private static readonly StructureTermService StructureTermService = new();

    public async Task LoadTermsAsync(string path)
    {
        var termsFiles = Directory.GetFiles(path).Select(Path.GetFullPath).ToList();
        var filesCategories = LoadCategories(termsFiles);
        await LoadAndStructureTermsAsync(termsFiles, filesCategories);

    }

    private async Task LoadAndStructureTermsAsync(List<string> termsFiles, Dictionary<string, int> filesCategories)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var termsFile in termsFiles)
        {
            try
            {
                var templateJson =  await File.ReadAllTextAsync(termsFile);
                if (string.IsNullOrWhiteSpace(templateJson))
                {
                    System.Console.WriteLine($"{termsFile} is empty.");
                    continue;
                }
                
                var parsedTermsFile = JsonSerializer.Deserialize<List<RawTerm>>(templateJson, options);

                if (parsedTermsFile is null)
                {
                    System.Console.WriteLine($"{termsFile} is null");
                    continue;
                }

                StructureTermsFromFile(parsedTermsFile, filesCategories[termsFile]);

            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{termsFile} is not valid JSON format.");
                throw;
            }
        }
    }

    private void StructureTermsFromFile(List<RawTerm> parsedTermsFile, int categoryId)
    {
        List<string> invalidTerms = [];

        foreach (var rawTerm in parsedTermsFile)
        {
            var term = StructureTermService.Invoke(rawTerm.Description, rawTerm.Term, categoryId);

            if (term is IValidTerm validTerm)
            {
                if (InMemoryDatabase.TermsDb.TryGetValue(validTerm.Value, out var value))
                {
                    value.Add(validTerm);
                }
                else
                {
                    InMemoryDatabase.TermsDb.Add(validTerm.Value, [validTerm]);
                }

                var length = validTerm.Value.Length;
                if (InMemoryDatabase.LengthTermsDb.TryGetValue(length, out var list))
                {
                    list.Add(validTerm);
                }
                else
                {
                    InMemoryDatabase.LengthTermsDb.Add(length, [validTerm]);
                }

                continue;
            }

            if (term is InvalidTerm invalidTerm)
            {
                invalidTerms.Add(invalidTerm.Error);
            }
            else
            {
                invalidTerms.Add("Unknown term issue");
            }
        }

        if (invalidTerms.Count > 0)
        {
            System.Console.WriteLine($"Invalid terms: {invalidTerms.Count}");
            foreach (var invalidTerm in invalidTerms)
            {
                System.Console.WriteLine(invalidTerm);
            }
        }


    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="termsFiles"></param>
    /// <returns>termsFile, categoryId</returns>
    private Dictionary<string, int> LoadCategories(List<string> termsFiles)
    {
        Dictionary<string, int> fileCategoryId = [];

        foreach (var termsFile in termsFiles)
        {
            var categoryName = Path.GetFileNameWithoutExtension(termsFile);
            var categoryId = 0;

            if (
                InMemoryDatabase.CategoriesDb.TryGetValue(categoryName, out var existingId))
            {
                categoryId = existingId;
            }
            else
            {
                categoryId = InMemoryDatabase.CategoriesDb.Values.Count > 0
                    ? InMemoryDatabase.CategoriesDb.Values.Max() + 1
                    : 1;
                InMemoryDatabase.CategoriesDb.Add(categoryName, categoryId);
            }

            fileCategoryId.Add(termsFile, categoryId);
        }

        return fileCategoryId;
    }
}
