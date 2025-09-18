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
    o.IncludeFormattedMessage = true;
    /*

    using (_logger.BeginScope(new Dictionary<string, object>
       {
           ["UserId"] = "fth-123",
           ["CorrelationId"] = "abc-xyz"
       }))
       {
           _logger.LogInformation("A new todo created.");
       }


     {
         "Message": "A new todo created.",
         "Scopes": [
           { "UserId": "fth-123", "CorrelationId": "abc-xyz" }
         ]
       }
     */


    o.AddOtlpExporter(); // endpoint varsayılan: http://localhost:4317
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