namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record RemoveIdeaItemIdRequest(
    string? ColumnName,
    long? ItemId,
    string? Changestamp);
