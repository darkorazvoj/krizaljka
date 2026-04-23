using Krizaljka.Domain;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.PostgreSql;
using Krizaljka.WebApi.Auth;
using Krizaljka.WebApi.Configs;
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

// Add services to the container.

    builder.Services.AddControllers();

    builder.Services.AddKrizaljkaDomain(o => { });
    builder.Services.AddKrizaljkaPostgreSql(o => o.ConnectionStringCore = connStringsConfig.Db);

    builder.Services.AddScoped<IAuthUser, AuthUserTemp>();

    var app = builder.Build();

// Configure the HTTP request pipeline.

    app.UseHttpsRedirection();

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