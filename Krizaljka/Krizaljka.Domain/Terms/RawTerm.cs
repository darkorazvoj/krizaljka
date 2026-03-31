using System.Text.Json.Serialization;

namespace Krizaljka.Domain.Terms;

public record RawTerm(
    [property: JsonPropertyName("o")]
    string Description, 
    [property: JsonPropertyName("w")]
    string Term);
