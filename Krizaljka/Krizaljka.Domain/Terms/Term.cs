
namespace Krizaljka.Domain.Terms;

public interface ITerm;

public interface INewTerm : ITerm
{
    TermLanguage Language { get; }
    string RawValue { get; }
    string DenseValue { get; }
    string SearchValue { get; }
    List<string> Letters { get; }
    int Length { get; }
    List<int> SpaceIndexes { get; }
    List<int> DashIndexes { get; }
    bool IsPrivate { get; }
}


public interface IValidTerm : ITerm
{
    long Id { get; }
    TermLanguage Language { get; }
    string RawValue { get; }
    string DenseValue { get; }
    IReadOnlyList<string> Letters { get; }
    int Length { get; }
    List<int> SpaceIndexes { get; }
    List<int> DashIndexes { get; }
    bool IsActive { get; }
    bool IsPrivate { get; }
    long CreatedById { get; }
    DateTimeOffset CreatedOn { get; }
    string Changestamp { get; }
}

public interface IInvalidTerm : ITerm
{
    string Error { get; }
}

public record Term(
    long Id,
    TermLanguage Language,
    long NumOfDesc,
    string RawValue,
    string DenseValue,
    IReadOnlyList<string> Letters,
    List<int> SpaceIndexes,
    List<int> DashIndexes,
    int Length,
    bool IsActive,
    bool IsPrivate,
    long? BatchId,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp) : IValidTerm;

public record TermExportItem(
    long Id,
    string Term,
    string Description);



public record NewTerm(
    TermLanguage Language,
    string RawValue,
    string DenseValue,
    string SearchValue,
    List<string> Letters,
    List<int> SpaceIndexes,
    List<int> DashIndexes,
    bool IsPrivate) : INewTerm
{
    public int Length => Letters.Count;
}

public record InvalidTerm(string Error): IInvalidTerm;

public record TermComputed(
    string TermCleaned,
    string DenseValue,
    string SearchValue,
    List<string> Letters,
    List<int> SpaceIndexes,
    List<int> DashIndexes,
    int Length): ITerm;