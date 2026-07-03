
namespace Krizaljka.Domain.Core.Stuff.Services;

internal interface IServiceValidationResult;

public record ValidationErrorsResult(List<string> Errors) : IServiceValidationResult;
