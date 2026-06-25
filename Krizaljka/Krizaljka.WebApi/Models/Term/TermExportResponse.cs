namespace Krizaljka.WebApi.Models.Term;

public record TermExportResponse(
    long Id,
    string W,
    List<string> Os);
