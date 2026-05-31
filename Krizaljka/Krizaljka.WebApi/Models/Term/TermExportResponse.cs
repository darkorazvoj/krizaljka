namespace Krizaljka.WebApi.Models.Term;

public record TermExportResponse(
    long Id,
    int Lang,
    string O,
    string W,
    bool IsActive);
