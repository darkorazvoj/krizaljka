namespace Krizaljka.Domain.Core.Stuff.Services;

public interface IServiceResult;

public record InvalidRequestWithReason(string Error = "") : IServiceResult;
public record SuccessInsert<T>(T Id) : IServiceResult;

public record Error(string Message) : IServiceResult;
public record NoAuthUser : IServiceResult;
