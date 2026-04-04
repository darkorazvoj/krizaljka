namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaIntersection(
    int FirstSlotId,
    int SecondSlotId,
    int Row,
    int Col,
    int FirstSlotCharIndex,
    int SecondSlotCharIndex);
