using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.Hashers;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.User.Models;
using Krizaljka.Domain.User.Repo;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.User.Services;

public class GetUserByCredentialsService(
    IAppUserRepo repo,
    IPasswordHasherService passHasher,
    KrizaljkaDomainOptions options,
    IServiceUser serviceUser,
    ILogger<GetUserByCredentialsService> logger)
{
    public async Task<IServiceResult> InvokeAsync(
        string? loginEmail,
        string? password,
        CancellationToken ct)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var attempt = 1;
        while (attempt <= 2)
        {
            attempt++;

            try
            {
                if (string.IsNullOrWhiteSpace(loginEmail) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    return new ValidationErrors(["LoginFailed"]);
                }

                loginEmail = loginEmail.Trim().ToLowerInvariant();

                var appUserAuth = await repo.GetByLoginEmailAsync(loginEmail.ToLowerInvariant(), ct);

                if (appUserAuth is null)
                {
                    // Timing attack prevention.
                    passHasher.VerifyHashedPassword("FakePasswordHash98765543221", password);
                    return new InvalidCredentials();
                }

                var verifyPassResult = passHasher.VerifyHashedPassword(appUserAuth.PasswordHash ?? "", password);
                if (verifyPassResult is not Success<(bool, bool)> { Data.Item1: true })
                {

                    if (appUserAuth.LoginFailedAttempt + 1 < options.MaxFailedLoginAttempts)
                    {
                        await repo.IncreaseLoginAttemptAsync(appUserAuth.Id, appUserAuth.Changestamp, serviceUser.Id,
                            ct);
                    }
                    else
                    {
                        await repo.IncreaseLoginAttemptAndBlockAsync(
                            appUserAuth.Id,
                            utcNow.AddMinutes(options.CoolOffTimeInMinutes),
                            appUserAuth.Changestamp,
                            serviceUser.Id,
                            ct);
                    }

                    return new InvalidCredentials();
                }

                var statusValidationResult = UserStatusValidation.Get(
                    appUserAuth.IsActive,
                    appUserAuth.EmailVerified,
                    appUserAuth.IsBlocked,
                    appUserAuth.BlockedUntil,
                    utcNow);

                if (statusValidationResult is ShouldUnblockUser)
                {
                    await repo.UnblockAsync(appUserAuth.Id, appUserAuth.Changestamp, serviceUser.Id, ct);
                    // Re-login again in case the unblock didn't work out or there is other reason why the user should not log in.
                    continue;
                }

                if (statusValidationResult is not Success)
                {
                    return statusValidationResult;
                }

                // Reset login attempts
                await repo.ResetLoginAttemptsAsync(appUserAuth.Id, appUserAuth.Changestamp, serviceUser.Id, ct);

                return new Success<AppUserMin>(new AppUserMin(
                    appUserAuth.Id,
                    appUserAuth.LoginEmail,
                    appUserAuth.EmailVerified,
                    appUserAuth.IsBlocked,
                    appUserAuth.BlockedUntil,
                    appUserAuth.IsActive,
                    appUserAuth.Changestamp));
            }
            catch (Exception e)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(e, "Get failed");
                }

                return new InvalidCredentials();
            }
        }

        return new InvalidCredentials();
    }
}
