using System.Text.Json;
using Krizaljka.Domain;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Console;

public record TermsLoaderResult(
    List<Term> ValidTerms, 
    List<string> InvalidTerms, 
    Dictionary<string, int> Categories);

public class TermsLoader
{
    public async Task<(List<Term> ValidTerms, List<string> invalidTerms, Dictionary<string, int> Categories)> LoadTermsAsync(string path)
    {
        var termsFiles = Directory.GetFiles(path).Select(Path.GetFullPath).ToList();
        var categoriesNameId = LoadCategories(termsFiles);
        var (validTerms, invalidTerms) =  await LoadAndStructureTermsAsync(termsFiles, categoriesNameId);

        return (validTerms, invalidTerms, categoriesNameId);
    }

    private async Task<(List<Term> ValidTerms, List<string> invalidTerms)>
        LoadAndStructureTermsAsync(List<string> termsFiles, Dictionary<string, int> categoriesNameId)
    {
        List<Term> validTerms = [];
        List<string> invalidTerms = [];

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var termsFile in termsFiles)
        {
            try
            {
                var templateJson = await File.ReadAllTextAsync(termsFile);
                if (string.IsNullOrWhiteSpace(templateJson))
                {
                    System.Console.WriteLine($"{termsFile} is empty.");
                    continue;
                }

                var parsedTermsFile = JsonSerializer.Deserialize<List<TermJson>>(templateJson, options);

                if (parsedTermsFile is null)
                {
                    System.Console.WriteLine($"{termsFile} is null");
                    continue;
                }

                var (valids, invalids) = StructureTermsFromFile(parsedTermsFile, categoriesNameId[Path.GetFileNameWithoutExtension(termsFile)]);
                validTerms.AddRange(valids);
                invalidTerms.AddRange(invalids);
            }
            catch
            {
                System.Console.WriteLine("TERM NOT LOADED");
                System.Console.WriteLine($"{termsFile} is not valid JSON format!!!!!!!!!!!!!!!!!!");
                return ([], []);
            }
        }

        return (validTerms, invalidTerms);
    }

    private (List<Term> ValidTerms, List<string> invalidTerms) StructureTermsFromFile(List<TermJson> parsedTermsFile,
        int categoryId)
    {
        List<Term> validTerms = [];
        List<string> invalidTerms = [];

        foreach (var rawTerm in parsedTermsFile)
        {
            var term = StructureTermService.Invoke(TermLanguage.Croatian, rawTerm.Description, rawTerm.Term,
                categoryId);

            if (term is IValidTerm validTerm)
            {
                validTerms.Add((Term)validTerm);
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

        return (validTerms, invalidTerms);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="termsFiles"></param>
    /// <returns>termsFile, categoryId</returns>
    private Dictionary<string, int> LoadCategories(List<string> termsFiles)
    {
        Dictionary<string, int> nameCategoryId = [];

        foreach (var termsFile in termsFiles)
        {
            var categoryName = Path.GetFileNameWithoutExtension(termsFile);
            nameCategoryId.Add(categoryName, CategoryIdGenerator.GetNextId());
        }

        return nameCategoryId;
        }
    }
