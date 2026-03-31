
namespace Krizaljka.Domain.Terms;

public interface ITerm;

public interface IValidTerm : ITerm
{
    string Description { get; }
    string Value { get; }
    int Length { get; }
    int CategoryId { get; }
    List<int> SpaceIndexes { get; }
    List<int> DashIndexes { get; }
}

public interface IInvalidTerm : ITerm
{
    string Error { get; }
}

public record Term(string Description,

    string Value,
    int Length,
    int CategoryId,
    List<int> SpaceIndexes,
    List<int> DashIndexes) : IValidTerm;

public record InvalidTerm(string Error): IInvalidTerm;