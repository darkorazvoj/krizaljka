using System.Text.Json.Serialization;

namespace Krizaljka.Domain.Terms;

public record TermMultipleDescriptionsJson(
    [property: JsonPropertyName("w")] 
    string Term,
    [property: JsonPropertyName("os")] 
    List<string> Description);
