namespace Krizaljka.Domain.Core.Stuff;

public interface IServiceUser
{
    public long Id { get; } 
    Task<bool> HasPermissionAsync(string permission, CancellationToken ct);
}
