using System;
using System.Collections.Generic;
using System.Text;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Template.Handlers;

public record GetKrizaljkaTemplatesPaginatedListServiceRequest(IPaginationCore Pagination) : IServiceRequest;

internal class GetKrizaljkaTemplatesPaginatedListHandler(
    IKrizaljkaTemplateRepo repo,
    ILogger<GetKrizaljkaTemplatesPaginatedListHandler> logger)
    : IAppRequestHandler<GetKrizaljkaTemplatesPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(
        GetKrizaljkaTemplatesPaginatedListServiceRequest request,
        CancellationToken ct)
    {
        try
        {
            var template = await repo.GetAsync(request.Id, ct);

            return template is null ? new NoData() : new Success<KrizaljkaTemplate>(template);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get Krizaljka template failed");
            }

            return new Error(string.Empty);
        }
    }
}
