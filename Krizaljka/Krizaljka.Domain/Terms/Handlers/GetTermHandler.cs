using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms.Handlers;

public record GetTermServiceRequest(long Id) : IServiceRequest;


internal class GetTermHandler(
    ITermRepo repo,
    ILogger<GetTermHandler> logger)
    : IAppRequestHandler<GetTermServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetTermServiceRequest request, CancellationToken ct)
    {
        try
        {
            var template = await repo.GetAsync(request.Id, ct);

            return template is null ? new NoData() : new Success<Term>(template);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get term failed");
            }

            return new Error(string.Empty);
        }
    }
}
