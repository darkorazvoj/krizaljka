using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.TermDescription;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms;

public class InsertTermService(
    ITermRepo repo, 
    IDbSession<ConnStrings> dbSession, 
    InsertTermDescriptionService insertTermDescriptionService,
    ILogger<InsertTermService> logger)
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

        var languageId = (int)language.Value;

        if (string.IsNullOrWhiteSpace(term))
        {
            return new InvalidRequestWithReason("Missing term");
        }

        var structuredTerm = StructureNewTermService.Invoke(language.Value, term, isPrivate);

        if (structuredTerm is not INewTerm newTerm)
        {
            return new InvalidRequestWithReason("Invalid term");
        }

        return await dbSession.ExecuteInTransactionAsync(async cancellationToken =>
        {
            var existingTerm = await repo.GetByLanguageAndLettersAsync(languageId, newTerm.Letters, cancellationToken);
            if (existingTerm is not null)
            {
                return new RecordExists(existingTerm.Id);
            }

            var termInsertResult =
                await InsertTermAsync(languageId, isPrivate, newTerm, batchId, ranById, cancellationToken);

            if (termInsertResult is not SuccessInsert<long> successTermInsertResult)
            {
                return termInsertResult;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return successTermInsertResult;
            }

            var descriptionInsertResult = await insertTermDescriptionService.InvokeAsync(
                successTermInsertResult.Id,
                description,
                batchId,
                ranById,
                cancellationToken);

            if (descriptionInsertResult is not SuccessInsert<long>)
            {
                return descriptionInsertResult;
            }

            return successTermInsertResult;

        }, ConnStrings.Core, ct);

    }

    private async Task<IServiceResult> InsertTermAsync(
        int languageId, 
        bool isPrivate, 
        INewTerm newTerm, 
        long? batchId, 
        long ranById,
        CancellationToken ct)
    {
        try
        {
            var id = await repo.InsertAsync(
                languageId,
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

            return new SuccessInsert<long>(id);
        }
        catch (Exception e)
        {
            logger.LogError("Error inserting a term. {EMessage}", e.Message);
            return new Error("InsertTermServiceFailed");
        }
    }
}
