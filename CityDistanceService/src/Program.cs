using FluentMigrator.Runner;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc($"{Constants.Version}", new OpenApiInfo
    {
        Version = $"{Constants.Version}",
        Title = "City Distance Service",
        Description = "A simple service to manage city information and calculate distances between cities.",
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

configuration.AddEnvironmentVariables();
var connectionString = configuration["DATABASE_CONNECTION_STRING"];
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = "Server=db;Database=CityDistanceService;Uid=root;Pwd=changeme";
    Console.WriteLine("DATABASE_CONNECTION_STRING environment variable not set.");
}

builder.Services.AddScoped<IDatabaseManager>(provider => new MySQLManager(connectionString));

// Configure FluentMigrator
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddMySql5()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(Program).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CityInfo>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"/swagger/{Constants.Version}/swagger.json", $"Documentation for City Distance Service version {Constants.Version}");
});

app.UseRouting();

app.UseStaticFiles();
app.MapControllers();

// Run migrations with retry logic at startup
try
{
    RetryHelper.RetryOnException(10, TimeSpan.FromSeconds(10), () =>
    {
        // Run migrations at startup
        using (var scope = app.Services.CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }
    });
}
catch (Exception ex)
{
    Console.WriteLine("Error running migrations: " + ex.Message);
    return;
}

app.UseMiddleware<ApplicationVersionMiddleware>();

// app.UseHttpsRedirection();
app.UseCors("AllowAll");
var endpointGroup = app.MapGroup("/city").AddFluentValidationAutoValidation();

Console.WriteLine("App version: " + Constants.Version);

if (string.IsNullOrEmpty(Constants.Version))
{
    Console.WriteLine("No version found error.");
    return;
}

app.MapGet("/health_check", () =>
{
    return Results.Ok();
});
app.MapGet("/db_health_check", async (IDatabaseManager dbManager) =>
{
    return await RequestHandler.TestConnection(dbManager);
});
app.MapGet("/version", () =>
{
    return Results.Ok(Constants.Version);
});

app.MapGet("/city/{id}", async ([FromRoute] Guid id, IDatabaseManager dbManager) =>
{
    return await RequestHandler.ValidateAndReturnCityInfoAsync(id, dbManager);
});
app.MapGet("/search/{name}", async ([FromRoute] string name, IDatabaseManager dbManager) =>
{
    return await RequestHandler.ValidateAndReturnCitiesCloseMatchAsync(name, dbManager);
});

app.MapPost("/city", async (CityInfo city, IDatabaseManager dbManager) =>
{
    return await RequestHandler.ValidateAndPostCityInfoAsync(city, dbManager);
});
app.MapPost("/distance", async (CitiesDistanceRequest cities, IDatabaseManager dbManager) =>
{
    return await RequestHandler.ValidateAndProcessCityDistanceAsync(cities, dbManager);
}).AddFluentValidationAutoValidation();

app.MapPut("/city", async (CityInfo city, IDatabaseManager dbManager) =>
{
    return await RequestHandler.ValidateAndUpdateCityInfoAsync(city, dbManager);
});

app.MapDelete("/city/{id}", async ([FromRoute] Guid id, IDatabaseManager dbManager) =>
{
    return await RequestHandler.ValidateAndDeleteCityAsync(id, dbManager);
});

app.Run();
