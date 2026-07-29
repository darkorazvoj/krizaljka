using Krizaljka.Domain.Core.Stuff.Pagination;

namespace Krizaljka.Domain.Idea;

public interface IKrizaljkaIdeaRepo
{
    Task<string?> InsertAsync(
        int languageId,
        int status,
        string themeName,
        int templateRows,
        int templateCols,
        int templateZeroBlocksNum,
        List<long> themeTerms,
        List<long> otherTerms,
        int minutesPerTemplate,
        int maxNumOfCompletedTemplates,
        List<long> templateIdsOnly,
        List<long> templateIdsExcluded,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct
    );

    Task<PaginatedResult<List<KrizaljkaIdeaListItem>>> GetListAsync(
        IPaginationCore paginationCore,
        CancellationToken ct);

    Task<PaginatedResult<List<KrizaljkaIdeaTermListItem>>> GetTermsListAsync(
        IPaginationCore paginationCore,
        List<(string, object?)>? additionalDynamicParameters,
        CancellationToken ct);

    Task<PaginatedResult<List<KrizaljkaIdeaTemplateListItem>>> GetTemplatesListAsync(
        IPaginationCore paginationCore,
        List<(string, object?)>? additionalDynamicParameters,
        CancellationToken ct);

    Task<PaginatedResult<List<KrizaljkaIdeaAllTemplatesListItem>>> GetAllTemplatesListAsync(
        IPaginationCore paginationCore,
        List<(string, object?)>? additionalDynamicParameters,
        CancellationToken ct);

    Task<KrizaljkaIdeaConfig?> GetConfigAsync(string id, CancellationToken ct);

    Task<string?> UpdateConfigAsync(
        string id,
        int languageId,
        string themeName,
        int templateRows,
        int templateCols,
        int templateZeroBlocksNum,
        int minutesPerTemplate,
        int maxNumOfCompletedTemplates,
        string changestamp,
        CancellationToken ct
    );

    Task<string?> AddIdAsync(
        string id,
        string columnName,
        long newId,
        string changestamp,
        CancellationToken ct
    );

    Task<string?> RemoveIdAsync(
        string id,
        string columnName,
        long newId,
        string changestamp,
        CancellationToken ct
    );
}
