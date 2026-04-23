using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Template.Handlers;

public record InsertKrizaljkaTemplateServiceRequest(
    int[][]? Matrix,
    string? Name) : IServiceRequest;

internal class InsertKrizaljkaTemplateHandler(
    IAuthUser authUser,
    IKrizaljkaTemplateRepo repo)
    : IAppRequestHandler<InsertKrizaljkaTemplateServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(InsertKrizaljkaTemplateServiceRequest request, CancellationToken ct)
    {
        if (request.Matrix is null)
        {
            return new InvalidRequestWithReason("Missing matrix");
        }

        if (request.Matrix.Length <= 0)
        {
            return new InvalidRequestWithReason("Matrix can't be empty");
        }

        var rowsCount = request.Matrix.Length;
        var columnsCount = request.Matrix[0].Length;

        var areColumnsConsistent = true;
        for (var r = 0; r < rowsCount; r++)
        {
            if (request.Matrix[r].Length != columnsCount)
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
            var id = await repo.InsertAsync(
                request.Matrix,
                request.Name,
                rowsCount,
                columnsCount,
                authUser.Id,
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

