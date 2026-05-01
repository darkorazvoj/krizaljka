using Krizaljka.Domain;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.PostgreSql;
using Krizaljka.WebApi.Auth;
using Krizaljka.WebApi.Configs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting Krizaljka WebApi!");

    var builder = WebApplication.CreateBuilder(args);

    ConnectionStringsConfig connStringsConfig = new();
    builder.Configuration
        .GetSection(ConnectionStringsConfig.SectionName)
        .Bind(connStringsConfig);

    if (string.IsNullOrWhiteSpace(connStringsConfig.Db))
    {
        throw new Exception("Missing connection string(s)!");
    }

    CorsConfig corsConfig = new();
    builder.Configuration.GetSection(CorsConfig.SectionName).Bind(corsConfig);

    const string corsPolicyName = "FrontendCors";
    const string authCookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

// Add services to the container.

    builder.Services.AddControllers();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IAuthUser, AuthUser>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(corsPolicyName, policy =>
        {
            // SECURITY - never allow "*" when using cookies/credentials
            if (corsConfig.AllowedOrigins.Length == 0)
            {
                // No cross-origin callers are allowed.
                // Same-origin requests will still work without CORS.
                policy.DisallowCredentials();
                return;
            }

            policy
                .WithOrigins(corsConfig.AllowedOrigins)
                .AllowCredentials()
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "X-CSRF", "Authorization", "session-id")
                .SetPreflightMaxAge(TimeSpan.FromHours(1));
        });
    });

    builder.Services
        .AddAuthentication(authCookieScheme)
        .AddCookie(authCookieScheme, options =>
        {
            options.Cookie.Name = "__kr_auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            // Use None only when frontend and backend are on different origins.
            // If same-site in production, prefer SameSiteMode.Lax.
            options.Cookie.SameSite = SameSiteMode.None;

            options.Cookie.Path = "/";

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);

            options.LoginPath = PathString.Empty;
            options.AccessDeniedPath = PathString.Empty;

            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();


    builder.Services.AddKrizaljkaDomain(_ => { });
    builder.Services.AddKrizaljkaPostgreSql(o =>
    {
        o.ConnectionStringCore = connStringsConfig.Db;
        o.ConnectionStringAu = connStringsConfig.Au;
    });

    builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
        new ConfigureOptions<KeyManagementOptions>(o =>
            o.XmlRepository = sp.GetRequiredService<IXmlRepository>()));

    // TODO - this has to be encrypted.
    builder.Services
        .AddDataProtection()
        .SetApplicationName("KrAu");

    builder.Services.AddSingleton<ServiceUser>();
    builder.Services.AddSingleton<IServiceUser>(sp => sp.GetRequiredService<ServiceUser>());

    var app = builder.Build();

    using (app.Services.CreateScope())
    {
        //var repo = scope.ServiceProvider.GetRequiredService<IServiceUserRepo>();
        var user = app.Services.GetRequiredService<ServiceUser>();

        try
        {
            // Set the value once at startup
            // This will fail if there are more than one service users and that is correct!
            //var serviceUserId = await repo.GetAsync(CancellationToken.None);
            //if (serviceUserId is null)
            //{
            //    throw new ArgumentException("Missing service user id!");
            //}

            user.Id = 7;// serviceUserId.Value;
        }
        catch (InvalidOperationException)
        {
            Log.Fatal("Service user not loaded!");
            throw;
        }
    }

// Configure the HTTP request pipeline.

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });


    app.UseHttpsRedirection();

    app.UseCors(corsPolicyName);

    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method))
        {
            await next();
            return;
        }

        string? origin = context.Request.Headers.Origin;

        if (string.IsNullOrWhiteSpace(origin))
        {
            if (app.Environment.IsDevelopment())
            {
                await next();
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (!corsConfig.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        //if (string.IsNullOrWhiteSpace(origin) ||
        //    !corsConfig.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        //{
        //    context.Response.StatusCode = StatusCodes.Status403Forbidden;
        //    return;
        //}

        await next();
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception e)
{
    Log.Fatal(e, "KABOOM!");

}
finally
{
    Log.Information("Byeeee!");
    Log.CloseAndFlush();
}