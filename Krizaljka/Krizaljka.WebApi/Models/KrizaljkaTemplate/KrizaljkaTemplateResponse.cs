
namespace Krizaljka.WebApi.Models.KrizaljkaTemplate;

public record KrizaljkaTemplateResponse(
    long Id,
    string? Name,
    int[][] Matrix,
    int RowsCount,
    int ColumnsCount,
    int NumZeroBlocks,
    List<TemplateBlockResponse> ZeroBlocks,
    bool IsActive,
    long CreatedById,
    DateTimeOffset? CreatedOn,
    string Changestamp);

