using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Starting Krizaljka WebApi!");


    var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

    builder.Services.AddControllers();

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