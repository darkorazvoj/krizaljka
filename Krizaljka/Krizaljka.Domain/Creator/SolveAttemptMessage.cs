
namespace Krizaljka.Domain.Creator;

public record SolveAttemptMessage(Guid ProcessId, KrizaljkaVersionARequest Request);
