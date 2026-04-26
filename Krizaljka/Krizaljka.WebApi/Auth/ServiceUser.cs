using Krizaljka.Domain.Core.Stuff;

namespace Krizaljka.WebApi.Auth;

internal class ServiceUser: IServiceUser
{
    public long Id { get; set; }

    public Task<bool> HasPermissionAsync(string permission, CancellationToken ct)
    {
        if (Id <= 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
