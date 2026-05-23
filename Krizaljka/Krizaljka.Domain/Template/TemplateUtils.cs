
namespace Krizaljka.Domain.Template;

internal static class TemplateUtils
{
    public static List<TemplateBlock> GetZeroBlocks(int[][]? matrix)
    {
        List<TemplateBlock> blocks = [];

        if (matrix is null || matrix.Length == 0 || matrix[0].Length == 0)
        {
            return blocks;
        }

        var rows = matrix.Length;
        var cols = matrix[0].Length;

        var used = new bool[rows, cols];

        for (var row = 0; row < rows; row++)
        {
            if (matrix[row].Length != cols)
            {
                continue;
            }

            for (var col = 0; col < cols; col++)
            {
                if (matrix[row][col] != 0 || used[row, col])
                {
                    continue;
                }

                var bestHeight = 0;
                var bestWidth = 0;
                var minWidth = int.MaxValue;

                for (var r = row; r < rows; r++)
                {
                    if (matrix[r].Length != cols)
                    {
                        break;
                    }

                    var width = 0;

                    while (col + width < cols
                           && matrix[r][col + width] == 0
                           && !used[r, col + width])
                    {
                        width++;
                    }

                    if (width == 0)
                    {
                        break;
                    }

                    minWidth = Math.Min(minWidth, width);

                    var height = r - row + 1;

                    if (minWidth >= 2 && height >= 2)
                    {
                        bestHeight = height;
                        bestWidth = minWidth;
                    }
                }

                if (bestHeight == 0)
                {
                    continue;
                }

                for (var r = row; r < row + bestHeight; r++)
                {
                    for (var c = col; c < col + bestWidth; c++)
                    {
                        used[r, c] = true;
                    }
                }

                blocks.Add(new TemplateBlock(
                    Row: row,
                    Col: col,
                    Height: bestHeight,
                    Width: bestWidth));
            }
        }

        return blocks;
    }
}
