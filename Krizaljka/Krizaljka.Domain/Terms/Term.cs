
namespace Krizaljka.Domain.Terms;

public interface ITerm;

public interface INewTerm : ITerm
{
    TermLanguage Language { get; }
    string Description { get; }
    string RawValue { get; }
    string DenseValue { get; }
    List<string> Letters { get; }
    int Length { get; }
    List<int> SpaceIndexes { get; }
    List<int> DashIndexes { get; }
}


public interface IValidTerm : ITerm
{
    long Id { get; }
    TermLanguage Language { get; }
    string Description { get; }
    string RawValue { get; }
    string DenseValue { get; }
    IReadOnlyList<string> Letters { get; }
    int Length { get; }
    List<int> SpaceIndexes { get; }
    List<int> DashIndexes { get; }
}

public interface IInvalidTerm : ITerm
{
    string Error { get; }
}

public record Term(
    long Id,
    TermLanguage Language,
    string Description,
    string RawValue,
    string DenseValue,
    IReadOnlyList<string> Letters,
    List<int> SpaceIndexes,
    List<int> DashIndexes) : IValidTerm
{
    public int Length => Letters.Count;
}

public record NewTerm(
    TermLanguage Language,
    string Description,
    string RawValue,
    string DenseValue,
    List<string> Letters,
    List<int> SpaceIndexes,
    List<int> DashIndexes) : INewTerm
{
    public int Length => Letters.Count;
}

public record InvalidTerm(string Error): IInvalidTerm;