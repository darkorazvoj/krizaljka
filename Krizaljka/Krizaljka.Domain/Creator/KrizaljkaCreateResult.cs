
namespace Krizaljka.Domain.Creator;

public record KrizaljkaCreateResult(
    bool IsCreated,
    KrizaljkaSolveState State,
    KrizaljkaSolveState BestState,
    KrizaljkaSolveStats Stats);
