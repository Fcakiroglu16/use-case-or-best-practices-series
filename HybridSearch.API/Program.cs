using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using HybridSearch.API.Configuration;
using HybridSearch.API.Endpoints;
using HybridSearch.API.Extensions;
using HybridSearch.API.Services.Implementations;
using HybridSearch.API.Services.Interfaces;
using MassTransit;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Elasticsearch settings
builder.Services.Configure<ElasticsearchSettings>(
    builder.Configuration.GetSection("Elasticsearch"));

// Register Elasticsearch client
builder.Services.AddSingleton(sp =>
{
    ElasticsearchSettings settings = sp.GetRequiredService<IOptions<ElasticsearchSettings>>().Value;

    var clientSettings = new ElasticsearchClientSettings(new Uri(settings.Uri));

    if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
    {
        clientSettings.Authentication(new BasicAuthentication(settings.Username, settings.Password));
    }

    return new ElasticsearchClient(clientSettings);
});

// Register Elasticsearch service
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

WebApplication app = builder.Build();

// Initialize Elasticsearch indexes
await app.Services.InitializeElasticsearchAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapArticlesEndpoints();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
