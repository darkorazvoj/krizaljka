namespace Krizaljka.WebApi.Workers.Models;

public interface IFileBatch;

public interface IFileContent
{
    string Content { get; }
}
public record FileContents<TFile>(List<TFile> Files)
    where TFile : IFileContent;


public record FileContent(string Content) : IFileContent;

public record TemplateFileBatch(List<FileContent> Contents) : IFileBatch;
public record TermFileBatch(List<FileContent> Contents) : IFileBatch;
