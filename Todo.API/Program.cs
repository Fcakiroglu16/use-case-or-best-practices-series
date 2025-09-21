using MassTransit;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;
using Todo.API.Middleware;
using Todo.API.OpenTelemetry;
using Todo.API.Todos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();

builder.Services.AddOpenTelemetryExt(builder.Configuration);

// OpenTelemetry logging provider
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeScopes = true;
    o.IncludeFormattedMessage = false;
    o.AddOtlpExporter();
});
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!));

        cfg.ConfigureEndpoints(context);
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<UserLoggingScopeMiddleware>();

// add open telemetry configuration


app.MapTodoEndpoints();
app.Run();