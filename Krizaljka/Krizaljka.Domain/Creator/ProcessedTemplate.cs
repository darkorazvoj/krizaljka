namespace Krizaljka.Domain.Creator;

public record ProcessedTemplate(long TemplateId, bool IsSolved, KrizaljkaSolveState State);
