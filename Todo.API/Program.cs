using Scalar.AspNetCore;
using Todo.API.OpenTelemetry;
using Todo.API.Todos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();

builder.Services.AddOpenTelemetryExt(builder.Configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


// add open telemetry configuration


app.MapTodoEndpoints();
app.Run();