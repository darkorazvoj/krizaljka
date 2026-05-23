

using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Template.Services;

public class InsertTemplateService(IKrizaljkaTemplateRepo repo)
{
    public async Task<IServiceResult> InvokeAsync(
        int[][]? matrix,
        string? name,
        long ranById,
        CancellationToken ct)
    {
        if (matrix is null)
        {
            return new InvalidRequestWithReason("Missing matrix");
        }

        if (matrix.Length <= 0 || matrix[0].Length <=0)
        {
            return new InvalidRequestWithReason("Matrix can't be empty");
        }

        var rowsCount = matrix.Length;
        var columnsCount = matrix[0].Length;

        var areColumnsConsistent = true;
        for (var r = 0; r < rowsCount; r++)
        {
            if (matrix[r].Length != columnsCount)
            {
                areColumnsConsistent = false;
                break;
            }
        }

        if (!areColumnsConsistent)
        {
            return new InvalidRequestWithReason("Inconsistent number of columns");
        }

        try
        {
            var matrixKey = MatrixKeyManager.CreateKey(matrix);

            var existing = await repo.GetByMatrixKeyAsync(matrixKey, ct);
            if (existing is not null)
            {
                return new RecordExists();
            }

            var zeroBlocks = TemplateUtils.GetZeroBlocks(matrix);

            var id = await repo.InsertAsync(
                matrix,
                matrixKey,
                name,
                rowsCount,
                columnsCount,
                zeroBlocks.Count,
                zeroBlocks,
                ranById,
                DateTimeOffset.UtcNow,
                ct);

            return new SuccessInsert<long>(id);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error inserting krizaljka template. {e.Message}");
            return new Error("InsertKrizaljkaTemplateFailed");
        }
    }
}
