
namespace Krizaljka.Domain.Template;

public sealed record TemplateBlock(
    int Row,
    int Col,
    int Height,
    int Width)
{
    public int Bottom => Row + Height - 1;
    public int Right => Col + Width - 1;
}
