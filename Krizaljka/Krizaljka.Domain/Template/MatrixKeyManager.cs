using System.Text;

namespace Krizaljka.Domain.Template;

internal static class MatrixKeyManager
{
    public static string CreateKey(int[][] matrix)
    {
        var rowsCount = matrix.Length;
        var colsCount = rowsCount == 0 ? 0 : matrix[0].Length;

        var sb = new StringBuilder(rowsCount * colsCount + 20);
        sb.Append(rowsCount)
            .Append('x')
            .Append(colsCount)
            .Append(':');


        foreach (var row in matrix)
        {
            foreach (var cell in row)
            {
                sb.Append(cell);
            }
        }

        return sb.ToString();
    }
}
