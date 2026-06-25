
using System.Text.Json.Serialization;

namespace Krizaljka.Domain.Terms;

public record TermExportImportJsonItem(
    [property: JsonPropertyName("w")]
    string Term,
    [property: JsonPropertyName("os")]
    List<string> Descriptions);
