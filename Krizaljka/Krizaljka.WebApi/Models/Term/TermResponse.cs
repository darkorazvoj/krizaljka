namespace Krizaljka.WebApi.Models.Term;

public record TermResponse(
    long Id,
    int Language,
    string Description,
    string RawValue,
    string DenseValue,
    IReadOnlyList<string> Letters,
    List<int> SpaceIndexes,
    List<int> DashIndexes,
    int Length,
    bool IsActive,
    bool IsPrivate,
    long CreatedById,
    DateTimeOffset CreatedOn,
    string Changestamp);
