using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Extensions;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.User.Models;
using Krizaljka.Domain.User.Services;

namespace Krizaljka.Domain.User.Handlers;

public record GetUserByUsernameServiceRequest(string? Username, string? Password) : IServiceRequest;

internal class GetUserByUsernameHandler(GetUserByCredentialsService getUserByCredentialsService)
    : IAppRequestHandler<GetUserByUsernameServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetUserByUsernameServiceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            !request.Username.IsValidEmailAddress())
        {
            return new ValidationErrors(["InvalidCredentials"]);
        }

        var result = await getUserByCredentialsService.InvokeAsync(request.Username, request.Password, ct);
        if (result is not Success<AppUserMin> successUser)
        {
            return result;
        }


        return new Success<long>(successUser.Data.Id);

    }
}
