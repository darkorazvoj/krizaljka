using System.Data.Common;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.Terms.Handlers;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.TermDescription.Handlers;


public record UpdateTermDescriptionServiceRequest(
    long? Id,
    string? Description,
    string? Changestamp) : IServiceRequest;

internal class UpdateTermDescriptionHandler(
    ITermDescriptionRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<UpdateTermHandler> logger) : IAppRequestHandler<UpdateTermDescriptionServiceRequest>
{
    private const int DescriptionMaxLength = 40;

    public async Task<IServiceResult> HandleAsync(UpdateTermDescriptionServiceRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        if (!request.Id.HasValue)
        {
            errors.Add("missing_id");
        }

        if (string.IsNullOrWhiteSpace(request.Changestamp))
        {
            errors.Add("missing_changestamp");
        }

        var descCleaned = request.Description?.TrimExtra() ?? string.Empty;

        if (descCleaned.Length > DescriptionMaxLength)
        {
            return new InvalidRequestWithReason("too_long");
        }

        if (errors.Count > 0)
        {
            return new ValidationErrors(errors);
        }

        GuardNotNull.Required(request.Id);
        GuardNotNull.Required(request.Changestamp);
        
        try
        {
            var newChangestamp =
                await repo.UpdateAsync(
                    request.Id.Value,
                    descCleaned,
                    request.Changestamp,
                    ct);

            return newChangestamp is null ? new Success(): new UpdateSuccessChangestamp<string>(newChangestamp);
        }
        catch (DbException e)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Update failed, database error. {sqlState}, {message}", e.SqlState, e.Message);
            }

            return dbUtils.GetSqlErrorResult(e.SqlState);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Term description update failed");
            }

            return new Error(string.Empty);
        }

    }
}
