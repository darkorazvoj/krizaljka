using System.Text.Json.Serialization;

namespace Krizaljka.Domain.Terms;

public record TermJson(
    [property: JsonPropertyName("o")]
    string Description, 
    [property: JsonPropertyName("w")]
    string Term);
