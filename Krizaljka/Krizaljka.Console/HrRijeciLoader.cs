using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Console;

public static class HrRijeciLoader
{
    private const string FileFullPath = @"C:\git\krizaljka\hrpojmovi\hrlista.txt";
    private const string SaveTermsPath = @"C:\git\krizaljka\hrpojmovi";

    private static readonly JsonSerializerOptions Options = new()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    public static void Load()
    {
        try
        {
            if (!File.Exists(FileFullPath))
            {
                System.Console.WriteLine("Di je lista?");
                return;
            }

            // Dictionary<int, int> numberOfWordPerNumberOfWordsInLine = [];
            HashSet<string> wordTypes = [];
            HashSet<string> glagoli = [];
            HashSet<string> veznici = [];
            HashSet<string> kratice = [];
            HashSet<string> imenice = [];
            HashSet<string> pridjevi = [];
            HashSet<string> prilozi = [];
            HashSet<string> uzvici = [];
            HashSet<string> cestice = [];
            HashSet<string> brojevi = [];
            HashSet<string> prijedlozi = [];
            HashSet<string> zamjenice = [];


            foreach (var line in File.ReadLines(FileFullPath))
            {
                var trimmedLine = line.TrimExtra();

                var words = trimmedLine
                    .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);


                var wordType = words[^1].ToUpper();
                wordTypes.Add(wordType);

                switch (wordType)
                {
                    case "GLAGOL":
                        glagoli.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "VEZNIK":
                        veznici.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "KRATICA":
                        kratice.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "IMENICA":
                        imenice.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "PRIDJEV":
                        pridjevi.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "PRILOG":
                        prilozi.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "UZVIK":
                        uzvici.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "ČESTICA":
                    case "(ČEST.)":
                        cestice.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "BROJ":
                        brojevi.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "PRIJEDLOG":
                        prijedlozi.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                    case "ZAMJENICA":
                        zamjenice.Add(words.Length > 1 ? words[1] : wordType);
                        break;
                }


                //if (numberOfWordPerNumberOfWordsInLine.TryGetValue(numOfWords, out var existingNum))
                //{
                //    numberOfWordPerNumberOfWordsInLine[numOfWords]++;
                //}
                //else
                //{
                //    numberOfWordPerNumberOfWordsInLine.Add(numOfWords, 1);
                //}


            }

            foreach (var wordType in wordTypes)
            {
                System.Console.WriteLine(wordType);
            }

            SaveGlagole("glagoli", glagoli);
            SaveGlagole("veznici", veznici);
            SaveGlagole("kratice", kratice);
            SaveGlagole("imenice", imenice);
            SaveGlagole("pridjevi", pridjevi);
            SaveGlagole("prilozi", prilozi);
            SaveGlagole("uzvici", uzvici);
            SaveGlagole("cestice", cestice);
            SaveGlagole("brojevi", brojevi);
            SaveGlagole("prijedlozi", prijedlozi);
            SaveGlagole("zamjenice", zamjenice);


            System.Console.WriteLine("Done!");
            System.Console.ReadKey();

            //foreach (var glagol in glagoli)
            //{
            //    System.Console.WriteLine(glagol);
            //}



            //System.Console.WriteLine("Num of words per line: ");
            //foreach (var kv in numberOfWordPerNumberOfWordsInLine)
            //{
            //    System.Console.WriteLine($"NumOfWordsPerLine: {kv.Key}, NumOfLines: {kv.Value}");
            //}
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
        }

    }

    private static void SaveGlagole(string name, HashSet<string> termsSet)
    {
        List<TermJson> terms = [];
        foreach (var term in termsSet)
        {
            terms.Add(new (string.Empty, term));
        }

        var termsJson = JsonSerializer.Serialize(terms, Options);

        File.WriteAllText(Path.Combine(SaveTermsPath, $"{name}.json"), termsJson);

    }
}
