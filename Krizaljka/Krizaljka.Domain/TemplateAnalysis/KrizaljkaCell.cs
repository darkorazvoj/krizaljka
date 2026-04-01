
using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.TemplateAnalysis;

public record KrizaljkaCell(
    int Row,
    int Col,
    KrizaljkaCellType Type);
