
namespace Krizaljka.Domain.Creator;

public record KrizaljkaCreateResult(
    bool IsCreated,
    KrizaljkaSolveState CurrentState,
    KrizaljkaSolveState BestState,
    KrizaljkaSolveStats Stats);
