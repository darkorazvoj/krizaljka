
using Microsoft.AspNetCore.Antiforgery;

namespace Krizaljka.WebApi.Csrf;


public sealed class CsrfEnforcementMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method) ||
            HttpMethods.IsDelete(context.Request.Method))
        {
            var endpoint = context.GetEndpoint();

            if (endpoint?.Metadata.GetMetadata<SkipCsrfAttribute>() == null)
            {
                try
                {
                    var antiforgery =
                        context.RequestServices.GetRequiredService<IAntiforgery>();

                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    context.Response.StatusCode = 419; 
                    context.Response.ContentType = "application/json";
                    
                    await context.Response.WriteAsJsonAsync(new { 
                        error = "CSRF_TOKEN_INVALID", 
                        message = "CSRF token is missing or invalid." 
                    });
                    
                    return; // STOP the pipeline here
                }
            }
        }

        await next(context);
    }
}
