
namespace Krizaljka.Domain.Terms;

public interface ITerm;

public interface IValidTerm : ITerm
{
    long Id { get; }
    TermLanguage Language { get; }
    string Description { get; }
    string RawValue { get; }
    IReadOnlyList<string> Letters { get; }
    int Length { get; }
    int CategoryId { get; }
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
    IReadOnlyList<string> Letters,
    int CategoryId,
    List<int> SpaceIndexes,
    List<int> DashIndexes) : IValidTerm
{
    public int Length => Letters.Count;
}

public record InvalidTerm(string Error): IInvalidTerm;