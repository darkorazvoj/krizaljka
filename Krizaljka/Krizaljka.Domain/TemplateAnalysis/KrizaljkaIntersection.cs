namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaIntersection(
    long FirstSlotId,
    long SecondSlotId,
    int Row,
    int Col,
    int FirstSlotCharIndex,
    int SecondSlotCharIndex);
