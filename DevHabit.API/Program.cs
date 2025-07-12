using DevHabit.API.Database;
using DevHabit.API.DTOs.Habits;
using DevHabit.API.Entities;
using DevHabit.API.Extensions;
using DevHabit.API.Middleware;
using DevHabit.API.Services;
using DevHabit.API.Services.Sorting;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// configuring another format as type of endpoint response from controller
builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true;
})
.AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = 
    new CamelCasePropertyNamesContractResolver()) // newtonsoft json
.AddXmlDataContractSerializerFormatters();

builder.Services.AddValidatorsFromAssemblyContaining<Program>(); // dependency injection fluend validation

// Problem details configuration
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier); // for open telemetry e.g.: aspire
    };
});
builder.Services.AddExceptionHandler<GLobalExceptionHandler>(); // middleware
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseNpgsql(
            builder.Configuration.GetConnectionString("Database"),
            npgsqlOptions => npgsqlOptions
                .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application))
        .UseSnakeCaseNamingConvention());

// Open Telemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddNpgsql()) // db telemetry
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation())
    .UseOtlpExporter();

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
});

// Sorting

builder.Services.AddTransient<SortMappingProvider>();

builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<HabitDto, Habit>>(_ =>
    HabitMappings.SortMapping);

builder.Services.AddTransient<DataShapingService>();

// =============

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await app.ApplyMigrationsAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Middleware
app.UseExceptionHandler();

app.MapControllers();

await app.RunAsync();
