
namespace Krizaljka.Domain.Solver;

public record KrizaljkaCreateResult(
    bool IsCreated,
    KrizaljkaSolveState State,
    int WordsTried);
