# ASP.NET Core API Implementation Guide

This document provides detailed implementation guidance for the ASP.NET Core API component in the Hybrid Search System.

---

## ?? Table of Contents

- [Overview](#overview)
- [Project Structure](#project-structure)
- [Dependencies](#dependencies)
- [Configuration](#configuration)
- [Implementation](#implementation)
  - [1. Models](#1-models)
  - [2. Services](#2-services)
  - [3. Controllers](#3-controllers)
  - [4. RabbitMQ Integration](#4-rabbitmq-integration)
  - [5. Elasticsearch Integration](#5-elasticsearch-integration)
  - [6. Search Orchestration](#6-search-orchestration)
- [API Endpoints](#api-endpoints)
- [Error Handling](#error-handling)
- [Testing](#testing)

---

## Overview

The ASP.NET Core API serves as the main orchestrator in the hybrid search system with the following responsibilities:

- **Data Ingestion**: Accept article submissions and store them in Elasticsearch
- **Message Publishing**: Send indexing messages to RabbitMQ for Python AI service
- **Search Orchestration**: Coordinate parallel searches between Elasticsearch and Python AI service
- **Result Fusion**: Implement Reciprocal Rank Fusion (RRF) algorithm to merge results
- **API Gateway**: Provide RESTful endpoints for client applications

---

## Project Structure

```
HybridSearch.API/
??? Controllers/
?   ??? ArticlesController.cs
?   ??? SearchController.cs
??? Models/
?   ??? Article.cs
?   ??? ArticleCreateRequest.cs
?   ??? SearchRequest.cs
?   ??? SearchResult.cs
?   ??? RabbitMQ/
?       ??? ArticleIndexMessage.cs
??? Services/
?   ??? Interfaces/
?   ?   ??? IArticleService.cs
?   ?   ??? ISearchService.cs
?   ?   ??? IElasticsearchService.cs
?   ?   ??? IRabbitMQPublisher.cs
?   ?   ??? IPythonAIClient.cs
?   ??? Implementations/
?       ??? ArticleService.cs
?       ??? SearchService.cs
?       ??? ElasticsearchService.cs
?       ??? RabbitMQPublisher.cs
?       ??? PythonAIClient.cs
??? Configuration/
?   ??? ElasticsearchSettings.cs
?   ??? RabbitMQSettings.cs
?   ??? PythonAISettings.cs
??? Algorithms/
?   ??? ReciprocalRankFusion.cs
??? Extensions/
?   ??? ServiceCollectionExtensions.cs
??? Program.cs
```

---

## Dependencies

Add the following NuGet packages to your project:

```xml
<PackageReference Include="NEST" Version="7.17.5" />
<PackageReference Include="Elasticsearch.Net" Version="7.17.5" />
<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
<PackageReference Include="Polly" Version="8.3.1" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
```

Install via CLI:
```bash
dotnet add package NEST
dotnet add package Elasticsearch.Net
dotnet add package RabbitMQ.Client
dotnet add package Polly
dotnet add package Polly.Extensions.Http
```

---

## Configuration

### appsettings.json

```json
{
  "Elasticsearch": {
    "Uri": "http://localhost:9200",
    "DefaultIndex": "articles",
    "Username": "",
    "Password": ""
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "article-indexing-exchange",
    "ExchangeType": "direct",
    "QueueName": "embedding-queue",
    "RoutingKey": "article.index"
  },
  "PythonAI": {
    "BaseUrl": "http://localhost:8000",
    "SemanticSearchEndpoint": "/semantic-search",
    "TimeoutSeconds": 30
  },
  "Search": {
    "MaxResultsPerSource": 20,
    "FinalResultCount": 10,
    "RRFConstant": 60
  }
}
```

### Configuration Classes

```csharp
// Configuration/ElasticsearchSettings.cs
public class ElasticsearchSettings
{
    public string Uri { get; set; } = string.Empty;
    public string DefaultIndex { get; set; } = "articles";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

// Configuration/RabbitMQSettings.cs
public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "article-indexing-exchange";
    public string ExchangeType { get; set; } = "direct";
    public string QueueName { get; set; } = "embedding-queue";
    public string RoutingKey { get; set; } = "article.index";
}

// Configuration/PythonAISettings.cs
public class PythonAISettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SemanticSearchEndpoint { get; set; } = "/semantic-search";
    public int TimeoutSeconds { get; set; } = 30;
}
```

---

## Implementation

### 1. Models

#### Article.cs
```csharp
namespace HybridSearch.API.Models;

public class Article
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### ArticleCreateRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace HybridSearch.API.Models;

public class ArticleCreateRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(10000, MinimumLength = 50)]
    public string Content { get; set; } = string.Empty;
}
```

#### SearchRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace HybridSearch.API.Models;

public class SearchRequest
{
    [Required]
    [StringLength(500, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    [Range(1, 100)]
    public int MaxResults { get; set; } = 10;
}
```

#### SearchResult.cs
```csharp
namespace HybridSearch.API.Models;

public class SearchResult
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentSnippet { get; set; } = string.Empty;
    public double Score { get; set; }
    public SearchSource Source { get; set; }
}

public enum SearchSource
{
    Hybrid,
    FullText,
    Semantic
}

public class HybridSearchResponse
{
    public List<SearchResult> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public double SearchDurationMs { get; set; }
}
```

#### RabbitMQ/ArticleIndexMessage.cs
```csharp
namespace HybridSearch.API.Models.RabbitMQ;

public class ArticleIndexMessage
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

---

### 2. Services

#### IElasticsearchService.cs
```csharp
namespace HybridSearch.API.Services.Interfaces;

public interface IElasticsearchService
{
    Task<bool> IndexArticleAsync(Article article, CancellationToken cancellationToken = default);
    Task<List<SearchResult>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default);
    Task<Article?> GetArticleByIdAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<List<Article>> GetArticlesByIdsAsync(IEnumerable<Guid> articleIds, CancellationToken cancellationToken = default);
    Task<bool> DeleteArticleAsync(Guid articleId, CancellationToken cancellationToken = default);
}
```

#### ElasticsearchService.cs
```csharp
using Nest;
using Microsoft.Extensions.Options;
using HybridSearch.API.Configuration;
using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Services.Implementations;

public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly string _defaultIndex;

    public ElasticsearchService(IOptions<ElasticsearchSettings> settings)
    {
        var config = settings.Value;
        _defaultIndex = config.DefaultIndex;

        var connectionSettings = new ConnectionSettings(new Uri(config.Uri))
            .DefaultIndex(_defaultIndex)
            .DefaultMappingFor<Article>(m => m.IndexName(_defaultIndex));

        if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
        {
            connectionSettings.BasicAuthentication(config.Username, config.Password);
        }

        _client = new ElasticClient(connectionSettings);
    }

    public async Task<bool> IndexArticleAsync(Article article, CancellationToken cancellationToken = default)
    {
        var response = await _client.IndexDocumentAsync(article, cancellationToken);
        return response.IsValid;
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        var searchResponse = await _client.SearchAsync<Article>(s => s
            .Index(_defaultIndex)
            .Size(maxResults)
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(f => f
                        .Field(a => a.Title, boost: 2.0)
                        .Field(a => a.Content)
                    )
                    .Query(query)
                    .Type(TextQueryType.BestFields)
                    .Fuzziness(Fuzziness.Auto)
                )
            )
            .Highlight(h => h
                .Fields(
                    f => f.Field(a => a.Content)
                        .PreTags("<mark>")
                        .PostTags("</mark>")
                        .FragmentSize(150)
                        .NumberOfFragments(1)
                )
            ),
            cancellationToken
        );

        if (!searchResponse.IsValid)
            return new List<SearchResult>();

        return searchResponse.Hits.Select(hit => new SearchResult
        {
            ArticleId = hit.Source.Id,
            Title = hit.Source.Title,
            ContentSnippet = hit.Highlight?.ContainsKey("content") == true
                ? string.Join(" ", hit.Highlight["content"])
                : hit.Source.Content.Substring(0, Math.Min(150, hit.Source.Content.Length)),
            Score = hit.Score ?? 0,
            Source = SearchSource.FullText
        }).ToList();
    }

    public async Task<Article?> GetArticleByIdAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<Article>(articleId, g => g.Index(_defaultIndex), cancellationToken);
        return response.IsValid ? response.Source : null;
    }

    public async Task<List<Article>> GetArticlesByIdsAsync(IEnumerable<Guid> articleIds, CancellationToken cancellationToken = default)
    {
        var response = await _client.SearchAsync<Article>(s => s
            .Index(_defaultIndex)
            .Size(articleIds.Count())
            .Query(q => q
                .Ids(i => i.Values(articleIds.Select(id => new Id(id))))
            ),
            cancellationToken
        );

        return response.IsValid ? response.Documents.ToList() : new List<Article>();
    }

    public async Task<bool> DeleteArticleAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync<Article>(articleId, d => d.Index(_defaultIndex), cancellationToken);
        return response.IsValid;
    }
}
```

#### IRabbitMQPublisher.cs
```csharp
namespace HybridSearch.API.Services.Interfaces;

public interface IRabbitMQPublisher
{
    Task PublishArticleIndexMessageAsync(ArticleIndexMessage message);
}
```

#### RabbitMQPublisher.cs
```csharp
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using HybridSearch.API.Configuration;
using HybridSearch.API.Models.RabbitMQ;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Services.Implementations;

public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMQSettings _settings;

    public RabbitMQPublisher(IOptions<RabbitMQSettings> settings)
    {
        _settings = settings.Value;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: _settings.ExchangeType,
            durable: true,
            autoDelete: false
        );

        // Declare queue
        _channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        // Bind queue to exchange
        _channel.QueueBind(
            queue: _settings.QueueName,
            exchange: _settings.ExchangeName,
            routingKey: _settings.RoutingKey
        );
    }

    public Task PublishArticleIndexMessageAsync(ArticleIndexMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: _settings.RoutingKey,
            basicProperties: properties,
            body: body
        );

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
```

#### IPythonAIClient.cs
```csharp
namespace HybridSearch.API.Services.Interfaces;

public interface IPythonAIClient
{
    Task<List<SemanticSearchResult>> SemanticSearchAsync(string query, int maxResults, CancellationToken cancellationToken = default);
}

public class SemanticSearchResult
{
    public Guid ArticleId { get; set; }
    public double Score { get; set; }
}
```

#### PythonAIClient.cs
```csharp
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using HybridSearch.API.Configuration;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Services.Implementations;

public class PythonAIClient : IPythonAIClient
{
    private readonly HttpClient _httpClient;
    private readonly PythonAISettings _settings;

    public PythonAIClient(HttpClient httpClient, IOptions<PythonAISettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<List<SemanticSearchResult>> SemanticSearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        var request = new { query, max_results = maxResults };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_settings.SemanticSearchEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var results = JsonSerializer.Deserialize<List<SemanticSearchResult>>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return results ?? new List<SemanticSearchResult>();
    }
}
```

#### ISearchService.cs
```csharp
namespace HybridSearch.API.Services.Interfaces;

public interface ISearchService
{
    Task<HybridSearchResponse> HybridSearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
```

#### SearchService.cs
```csharp
using System.Diagnostics;
using HybridSearch.API.Algorithms;
using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Services.Implementations;

public class SearchService : ISearchService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IPythonAIClient _pythonAIClient;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IElasticsearchService elasticsearchService,
        IPythonAIClient pythonAIClient,
        ILogger<SearchService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _pythonAIClient = pythonAIClient;
        _logger = logger;
    }

    public async Task<HybridSearchResponse> HybridSearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Execute parallel searches
        var fullTextTask = _elasticsearchService.SearchAsync(request.Query, 20, cancellationToken);
        var semanticTask = _pythonAIClient.SemanticSearchAsync(request.Query, 20, cancellationToken);

        await Task.WhenAll(fullTextTask, semanticTask);

        var fullTextResults = await fullTextTask;
        var semanticResults = await semanticTask;

        _logger.LogInformation("Full-text search returned {Count} results", fullTextResults.Count);
        _logger.LogInformation("Semantic search returned {Count} results", semanticResults.Count);

        // Apply Reciprocal Rank Fusion
        var fusedResults = ReciprocalRankFusion.Fuse(fullTextResults, semanticResults);

        // Get top N results
        var topResults = fusedResults.Take(request.MaxResults).ToList();

        // Fetch full article details
        var articleIds = topResults.Select(r => r.ArticleId).ToList();
        var articles = await _elasticsearchService.GetArticlesByIdsAsync(articleIds, cancellationToken);

        // Map to final results
        var finalResults = topResults.Select(r =>
        {
            var article = articles.FirstOrDefault(a => a.Id == r.ArticleId);
            return new SearchResult
            {
                ArticleId = r.ArticleId,
                Title = article?.Title ?? r.Title,
                ContentSnippet = article?.Content.Substring(0, Math.Min(150, article.Content.Length)) ?? r.ContentSnippet,
                Score = r.Score,
                Source = SearchSource.Hybrid
            };
        }).ToList();

        stopwatch.Stop();

        return new HybridSearchResponse
        {
            Results = finalResults,
            TotalCount = finalResults.Count,
            SearchDurationMs = stopwatch.Elapsed.TotalMilliseconds
        };
    }
}
```

#### IArticleService.cs
```csharp
namespace HybridSearch.API.Services.Interfaces;

public interface IArticleService
{
    Task<Article> CreateArticleAsync(ArticleCreateRequest request, CancellationToken cancellationToken = default);
    Task<Article?> GetArticleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default);
}
```

#### ArticleService.cs
```csharp
using HybridSearch.API.Models;
using HybridSearch.API.Models.RabbitMQ;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Services.Implementations;

public class ArticleService : IArticleService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(
        IElasticsearchService elasticsearchService,
        IRabbitMQPublisher rabbitMQPublisher,
        ILogger<ArticleService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _rabbitMQPublisher = rabbitMQPublisher;
        _logger = logger;
    }

    public async Task<Article> CreateArticleAsync(ArticleCreateRequest request, CancellationToken cancellationToken = default)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        // Store in Elasticsearch
        var indexed = await _elasticsearchService.IndexArticleAsync(article, cancellationToken);
        if (!indexed)
        {
            _logger.LogError("Failed to index article {ArticleId}", article.Id);
            throw new Exception("Failed to index article");
        }

        // Publish to RabbitMQ for embedding generation
        var message = new ArticleIndexMessage
        {
            ArticleId = article.Id,
            Title = article.Title,
            Content = article.Content,
            Timestamp = article.CreatedAt
        };

        await _rabbitMQPublisher.PublishArticleIndexMessageAsync(message);

        _logger.LogInformation("Article {ArticleId} created and queued for indexing", article.Id);

        return article;
    }

    public async Task<Article?> GetArticleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _elasticsearchService.GetArticleByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _elasticsearchService.DeleteArticleAsync(id, cancellationToken);
    }
}
```

---

### 3. Controllers

#### ArticlesController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(IArticleService articleService, ILogger<ArticlesController> logger)
    {
        _articleService = articleService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new article
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Article), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Article>> CreateArticle([FromBody] ArticleCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var article = await _articleService.CreateArticleAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article");
            return BadRequest(new { error = "Failed to create article" });
        }
    }

    /// <summary>
    /// Get article by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Article), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Article>> GetArticle(Guid id, CancellationToken cancellationToken)
    {
        var article = await _articleService.GetArticleAsync(id, cancellationToken);
        
        if (article == null)
            return NotFound();

        return Ok(article);
    }

    /// <summary>
    /// Delete article by ID
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArticle(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _articleService.DeleteArticleAsync(id, cancellationToken);
        
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
```

#### SearchController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Perform hybrid search combining full-text and semantic search
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="maxResults">Maximum number of results (default: 10)</param>
    [HttpGet]
    [ProducesResponseType(typeof(HybridSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HybridSearchResponse>> Search(
        [FromQuery] string q,
        [FromQuery] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required" });

        var request = new SearchRequest
        {
            Query = q,
            MaxResults = maxResults
        };

        try
        {
            var results = await _searchService.HybridSearchAsync(request, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hybrid search for query: {Query}", q);
            return StatusCode(500, new { error = "An error occurred while performing search" });
        }
    }
}
```

---

### 4. RabbitMQ Integration

The RabbitMQ integration has already been covered in the `RabbitMQPublisher` service above. Key points:

- **Exchange Type**: Direct exchange for targeted routing
- **Durable**: Messages and queues persist across broker restarts
- **Routing Key**: Used to route messages to the correct queue
- **Message Format**: JSON serialization for interoperability with Python service

---

### 5. Elasticsearch Integration

The Elasticsearch integration has been covered in the `ElasticsearchService`. Key features:

- **NEST Client**: Official .NET client for Elasticsearch
- **Multi-Match Query**: Searches across multiple fields (title, content)
- **Field Boosting**: Title field boosted 2x for relevance
- **Fuzziness**: Handles typos automatically
- **Highlighting**: Returns highlighted snippets for better UX

---

### 6. Search Orchestration

#### Algorithms/ReciprocalRankFusion.cs

```csharp
using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;

namespace HybridSearch.API.Algorithms;

public static class ReciprocalRankFusion
{
    private const int DefaultK = 60;

    public static List<SearchResult> Fuse(
        List<SearchResult> fullTextResults,
        List<SemanticSearchResult> semanticResults,
        int k = DefaultK)
    {
        var scores = new Dictionary<Guid, double>();
        var resultDetails = new Dictionary<Guid, SearchResult>();

        // Process full-text results
        for (int i = 0; i < fullTextResults.Count; i++)
        {
            var result = fullTextResults[i];
            var rank = i + 1;
            var rrfScore = 1.0 / (k + rank);

            if (!scores.ContainsKey(result.ArticleId))
            {
                scores[result.ArticleId] = 0;
                resultDetails[result.ArticleId] = result;
            }

            scores[result.ArticleId] += rrfScore;
        }

        // Process semantic results
        for (int i = 0; i < semanticResults.Count; i++)
        {
            var result = semanticResults[i];
            var rank = i + 1;
            var rrfScore = 1.0 / (k + rank);

            if (!scores.ContainsKey(result.ArticleId))
            {
                scores[result.ArticleId] = 0;
                resultDetails[result.ArticleId] = new SearchResult
                {
                    ArticleId = result.ArticleId,
                    Score = result.Score,
                    Source = SearchSource.Semantic
                };
            }

            scores[result.ArticleId] += rrfScore;
        }

        // Sort by combined RRF score and return
        return scores
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp =>
            {
                var result = resultDetails[kvp.Key];
                result.Score = kvp.Value;
                result.Source = SearchSource.Hybrid;
                return result;
            })
            .ToList();
    }
}
```

**RRF Algorithm Explanation:**

The Reciprocal Rank Fusion algorithm combines rankings from multiple search systems:

```
RRF_Score = ? (1 / (k + rank_i))
```

Where:
- `k` = constant (typically 60) to prevent division by zero and reduce impact of top results
- `rank_i` = position in the i-th ranking list

**Benefits:**
- No normalization needed (works with different score scales)
- Rank-based (position matters more than absolute scores)
- Simple and effective for combining heterogeneous search results

---

## API Endpoints

### Articles

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/articles` | Create a new article |
| GET | `/api/articles/{id}` | Get article by ID |
| DELETE | `/api/articles/{id}` | Delete article by ID |

### Search

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/search?q={query}&maxResults={n}` | Hybrid search |

### Example Requests

**Create Article:**
```bash
curl -X POST http://localhost:5000/api/articles \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Introduction to Machine Learning",
    "content": "Machine learning is a subset of artificial intelligence..."
  }'
```

**Search:**
```bash
curl http://localhost:5000/api/search?q=machine%20learning&maxResults=10
```

---

## Error Handling

### Global Exception Handler

Add to `Program.cs`:

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception");

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An internal error occurred",
                details = app.Environment.IsDevelopment() ? error.Error.Message : null
            });
        }
    });
});
```

---

## Testing

### Unit Test Example

```csharp
using Xunit;
using Moq;
using HybridSearch.API.Services.Implementations;
using HybridSearch.API.Services.Interfaces;

public class SearchServiceTests
{
    [Fact]
    public async Task HybridSearch_CombinesResults_Successfully()
    {
        // Arrange
        var mockElastic = new Mock<IElasticsearchService>();
        var mockPython = new Mock<IPythonAIClient>();
        var mockLogger = new Mock<ILogger<SearchService>>();

        mockElastic.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), default))
            .ReturnsAsync(new List<SearchResult>
            {
                new SearchResult { ArticleId = Guid.NewGuid(), Score = 10.5 }
            });

        mockPython.Setup(x => x.SemanticSearchAsync(It.IsAny<string>(), It.IsAny<int>(), default))
            .ReturnsAsync(new List<SemanticSearchResult>
            {
                new SemanticSearchResult { ArticleId = Guid.NewGuid(), Score = 0.95 }
            });

        var service = new SearchService(mockElastic.Object, mockPython.Object, mockLogger.Object);

        // Act
        var result = await service.HybridSearchAsync(new SearchRequest { Query = "test", MaxResults = 10 });

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Results.Count > 0);
    }
}
```

---

## Program.cs Configuration

```csharp
using HybridSearch.API.Configuration;
using HybridSearch.API.Services.Implementations;
using HybridSearch.API.Services.Interfaces;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add configurations
builder.Services.Configure<ElasticsearchSettings>(builder.Configuration.GetSection("Elasticsearch"));
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<PythonAISettings>(builder.Configuration.GetSection("PythonAI"));

// Add services
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();
builder.Services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Add HTTP client with retry policy for Python AI service
builder.Services.AddHttpClient<IPythonAIClient, PythonAIClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(GetRetryPolicy());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

---

## Summary

This implementation provides:

? **Complete ASP.NET Core API** for hybrid search system  
? **Elasticsearch integration** for full-text search  
? **RabbitMQ integration** for asynchronous message publishing  
? **HTTP client** for Python AI service communication  
? **RRF algorithm** for result fusion  
? **Resilient HTTP calls** with Polly retry policies  
? **Clean architecture** with service interfaces  
? **Comprehensive error handling**  
? **Ready for production** deployment  

Next steps: Implement the Python AI service and set up infrastructure (Elasticsearch, RabbitMQ, Pinecone).
