using System.Data.Common;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Terms;
using Krizaljka.Domain.Terms.Handlers;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;


public record UpdateKrizaljkaIdeaConfigServiceRequest(
    string? Id, 
    int? LanguageId,
    string? ThemeName, 
    int? TemplateRows, 
    int? TemplateCols,
    int? TemplateZeroBlocksNum,
    int? MinutesPerTemplate, 
    int? MaxNumOfCompletedTemplates, 
    string? Changestamp) : IServiceRequest;

internal class UpdateKrizaljkaIdeaConfigHandler(
    ITermRepo repo,
    IDatabaseUtils dbUtils,
    ILogger<UpdateKrizaljkaIdeaConfigHandler> logger) : IAppRequestHandler<UpdateKrizaljkaIdeaConfigServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(UpdateKrizaljkaIdeaConfigServiceRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            errors.Add("missing_id");
        }

        if (string.IsNullOrWhiteSpace(request.Term))
        {
            errors.Add("missing_term");
        }

        if (string.IsNullOrWhiteSpace(request.Changestamp))
        {
            errors.Add("missing_changestamp");
        }

        if (errors.Count > 0)
        {
            return new ValidationErrors(errors);
        }

        GuardNotNull.Required(request.Id);
        GuardNotNull.Required(request.Term);
        GuardNotNull.Required(request.Changestamp);

        var term = StructureTermService.Invoke(request.Term);

        if (term is not TermComputed termComputed)
        {
            return new InvalidRequestWithReason("Invalid term");
        }

        try
        {
            var newChangestamp =
                await repo.UpdateTermAsync(
                    request.Id.Value,
                    termComputed.TermCleaned,
                    termComputed.DenseValue,
                    termComputed.SearchValue,
                    termComputed.Letters,
                    termComputed.SpaceIndexes,
                    termComputed.DashIndexes,
                    termComputed.Length,
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

            return e.SqlState == IDatabaseUtils.InvalidChangestampCode
                ? new InvalidChangestamp()
                : new InvalidRequestWithReason(dbUtils.MapSqlStateToError(e.SqlState));
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Term update failed");
            }

            return new Error(string.Empty);
        }



    }
}
