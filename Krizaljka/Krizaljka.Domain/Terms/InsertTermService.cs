using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms;

public class InsertTermService(ITermRepo repo, IDbSession<ConnStrings> dbSession, ILogger<InsertTermService> logger)
{
    public async Task<IServiceResult> InvokeAsync(
        TermLanguage? language,
        string? description,
        string? term,
        bool isPrivate,
        long? batchId,
        long ranById,
        CancellationToken ct)
    {
        if (language is null)
        {
            return new InvalidRequestWithReason("Missing language");
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            return new InvalidRequestWithReason("Missing term");
        }

        var structuredTerm = StructureNewTermService.Invoke(language.Value, description ?? string.Empty, term, isPrivate);

        if (structuredTerm is not INewTerm newTerm)
        {
            return new InvalidRequestWithReason("Invalid term");
        }

        try
        {
            var id = await repo.InsertAsync(
                (int)language.Value,
                newTerm.Description,
                newTerm.RawValue,
                newTerm.DenseValue,
                newTerm.Letters,
                newTerm.SpaceIndexes,
                newTerm.DashIndexes,
                newTerm.Length,
                isPrivate,
                batchId,
                ranById,
                DateTimeOffset.UtcNow,
                ct);

            if (!string.IsNullOrWhiteSpace(description))
            {
                // TODO insert description.
            }

            return new SuccessInsert<long>(id);
        }
        catch (Exception e)
        {
            logger.LogError("Error inserting a term. {EMessage}", e.Message);
            return new Error("InsertTermServiceFailed");
        }

    }
}
