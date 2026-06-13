namespace Krizaljka.Domain.Core.Stuff.Services;

public interface IServiceResult;

public interface ICommittableResult;

public record Success: IServiceResult, ICommittableResult;

public record InvalidRequestWithReason(string Error = "") : IServiceResult;
public record SuccessInsert<T>(T Id) : IServiceResult, ICommittableResult; 
public record Success<T>(T Data) : IServiceResult, ICommittableResult;
public record UpdateSuccessChangestamp<T>(T Changestamp) : IServiceResult, ICommittableResult;

public record RecordExists : IServiceResult;
public record NoData : IServiceResult, ICommittableResult;

public record Error(string Message) : IServiceResult;
public record NoAuthUser : IServiceResult;
public record InvalidCredentials : IServiceResult;
public record InvalidChangestamp : IServiceResult;
public record ShouldUnblockUser: IServiceResult;

public record EmailNotVerified: IServiceResult;

public record DatabaseOperationFailed : IServiceResult;


public class ValidationErrors(List<string>? errors = null) : IServiceResult
{
    public List<string> Errors { get; } = errors ?? [];

    public ValidationErrors Add(string error)
    {
        Errors.Add(error);
        return this;
    }

    public ValidationErrors Add(List<string> errors)
    {
        Errors.AddRange(errors);
        return this;
    }
}
