
using Krizaljka.Domain.Template;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Krizaljka.Console;

public static class KrizaljkaTemplatesManager
{
    private const string TemplatesBasePath = @"C:\git\krizaljka\templates";
    private const string TemplatesDbPath = @"C:\git\krizaljka\templates\db";
    private const string ProcessedTemplatesPath = @"C:\git\krizaljka\templates\processed";
    private const string TemplatesDbNamePrefix = "KrizaljkaTemplates";
    private const int MaxTemplatesPerFile = 50;

    private static readonly JsonSerializerOptions Options = new()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), PropertyNameCaseInsensitive = true  };

    public static async Task<(int NumberOfTemplates, int NumberOfNewTemplates)> CreateTemplateDatabaseAsync()
    {
        if (!Directory.Exists(TemplatesBasePath))
        {
            Directory.CreateDirectory(TemplatesBasePath);
            return (0, 0);
        }

        if (!Directory.Exists(TemplatesDbPath))
        {
            Directory.CreateDirectory(TemplatesDbPath);
        }

        var existingTemplates = await LoadAllTemplatesAsync();
        var (newTemplates, newTemplatesFileNames) = await GetNewTemplatesAsync();

        var numOfNew = 0;
        if (newTemplates.Count > 0)
        {
            numOfNew = await AddNewToDbAsync(existingTemplates, newTemplates);
            MoveProcessedFiles(newTemplatesFileNames);
        }

        return (existingTemplates.Count, numOfNew);
    }

    public static async Task<KrizaljkaTemplatesDb> LoadTemplatesAsync()
    {
        var list = await LoadAllTemplatesAsync();
        return new KrizaljkaTemplatesDb(list);
    }

    private static void MoveProcessedFiles(List<string> newTemplatesFileNames)
    {
        if (!Directory.Exists(ProcessedTemplatesPath))
        {
            Directory.CreateDirectory(ProcessedTemplatesPath);
        }

        foreach (var name in newTemplatesFileNames)
        {
            var fileName = Path.GetFileNameWithoutExtension(name);
            var extension = Path.GetExtension(name);
            var destination = Path.Combine(ProcessedTemplatesPath, fileName + extension);

            var count = 1;
            while (File.Exists(destination))
            {
                var tempFileName = $"{fileName}_{count++}{extension}";
                destination = Path.Combine(ProcessedTemplatesPath, tempFileName);
            }

            File.Move(name, destination);
        }
    }

    private static async Task<int> AddNewToDbAsync(
        List<KrizaljkaTemplate> existingTemplates,
        List<KrizaljkaTemplate> newTemplates)
    {
        var nextFileId = GetNextFileId();
        var nextTemplateId = existingTemplates.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1;

        List<KrizaljkaTemplate> newTemplatesBatch = [];
        var currentBatchSize = 0;
        var currentBatchId = nextFileId;
        var numOfNewTemplates = 0;

        foreach (var newTemplate in newTemplates)
        {
            var exists = false;
            foreach (var existingTemplate in existingTemplates)
            {
                
                if (AreTemplatesEqual(existingTemplate, newTemplate))
                {
                    exists = true;
                    break;
                }
            }

            if (exists)
            {
                continue;
            }

            newTemplatesBatch.Add(newTemplate with{Id = nextTemplateId});
            nextTemplateId++;
            currentBatchSize++;
            numOfNewTemplates++;

            if (currentBatchSize >= MaxTemplatesPerFile)
            {
                await SaveDbFileAsync(newTemplatesBatch, currentBatchId);
                
                currentBatchId++;
                currentBatchSize = 0;
                newTemplatesBatch.Clear();
            }
        }


        if (newTemplatesBatch.Count > 0)
        {
            await SaveDbFileAsync(newTemplatesBatch, currentBatchId);
        }

        return numOfNewTemplates;

    }

    private static bool AreTemplatesEqual(
        KrizaljkaTemplate first, 
        KrizaljkaTemplate second)
    {
        if (first.Matrix.Length != second.Matrix.Length)
        {
            return false;

        }

        for (var r = 0; r < first.Matrix.Length; r++)
        {
            if (first.Matrix[r].Length != second.Matrix[r].Length)
            {
                return false;
            }

            for (var c = 0; c < first.Matrix[r].Length; c++)
            {
                if (first.Matrix[r][c] != second.Matrix[r][c])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static async Task SaveDbFileAsync(List<KrizaljkaTemplate> batch, int batchId)
    {
        var templatesDbJsonToWrite = JsonSerializer.Serialize(new KrizaljkaTemplatesDb(batch), Options);
        await File.WriteAllTextAsync(Path.Combine(TemplatesDbPath, $"{TemplatesDbNamePrefix}_{batchId:00000}.json"), templatesDbJsonToWrite);
    
    }

    private static int GetNextFileId()
    {
        var existingDbFileNames = GetExistingDbFiles();
        var currentMaxId =  existingDbFileNames
            .Select(f =>
            {
                var fileName = Path.GetFileNameWithoutExtension(f);

                var parts = fileName.Split(new[] { '_', '.' });
                if (parts.Length >= 2 && int.TryParse(parts[1], out var fileId))
                {
                    return fileId;
                }

                return 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return currentMaxId + 1;
    }

    private static async Task<List<KrizaljkaTemplate>> LoadAllTemplatesAsync()
    {
        List<KrizaljkaTemplate> templates = [];

        var existingDbFiles = GetExistingDbFiles();
        foreach (var dbFile in existingDbFiles)
        {
            var templatesJsonString = await File.ReadAllTextAsync(dbFile);
            if (string.IsNullOrWhiteSpace(templatesJsonString))
            {
                continue;
            }

            try
            {
                var templatesDb = JsonSerializer.Deserialize<KrizaljkaTemplatesDb>(templatesJsonString, Options);
                if (templatesDb?.Templates is null)
                {
                    continue;
                }

                templates.AddRange(templatesDb.Templates);
            }
            catch 
            {
                System.Console.WriteLine($"Invalid template DB file: {dbFile}");
            }
        }

        return templates;
    }

    private static List<string> GetExistingDbFiles()
    {
        if (!Directory.Exists(TemplatesDbPath))
        {
            return [];
        }

        var dbDirectory = new DirectoryInfo(TemplatesDbPath);
        var existingDbFiles = dbDirectory.GetFiles($"{TemplatesDbNamePrefix}_*.json");
        return existingDbFiles.Select(x => x.FullName)
            .ToList();
    }

    private static async Task<(List<KrizaljkaTemplate> Templates, List<string> TemplatesFileNames)> GetNewTemplatesAsync()
    {
        List<KrizaljkaTemplate> templates = [];
        List<string> templatesFileNames = [];

        var newTemplatesFileNames = GetNewTemplateFiles();
        foreach (var templateName in newTemplatesFileNames)
        {
            var templateJsonString = await File.ReadAllTextAsync(templateName);
            if (string.IsNullOrWhiteSpace(templateJsonString))
            {
                continue;
            }

            try
            {
                var template = JsonSerializer.Deserialize<KrizaljkaTemplate>(templateJsonString, Options);
                if (template?.Matrix is null)
                {
                    continue;
                }

                var isDuplicate = false;
                if (templates.Count > 0)
                {
                    foreach (var existingTemplate in templates)
                    {
                        if (AreTemplatesEqual(existingTemplate, template))
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                }

                templatesFileNames.Add(templateName);

                if (isDuplicate)
                {
                    continue;
                }

                templates.Add(template);
            }
            catch
            {
                System.Console.WriteLine($"Invalid template JSON: {templateName}");
            }
        }

        return (templates, templatesFileNames);
    }

    private static List<string> GetNewTemplateFiles() =>
        !Directory.Exists(TemplatesBasePath) ? [] : Directory.GetFiles(TemplatesBasePath).ToList();
}


