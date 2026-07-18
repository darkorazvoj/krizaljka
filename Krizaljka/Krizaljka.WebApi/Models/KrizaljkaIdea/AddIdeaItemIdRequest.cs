namespace Krizaljka.WebApi.Models.KrizaljkaIdea;

public record AddIdeaItemIdRequest(
    string? ColumnName,
    long? NewId,
    string? Changestamp);
