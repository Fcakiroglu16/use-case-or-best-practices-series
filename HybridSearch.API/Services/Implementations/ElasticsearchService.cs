using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using HybridSearch.API.Configuration;
using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace HybridSearch.API.Services.Implementations;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly string _defaultIndex;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(IOptions<ElasticsearchSettings> settings, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        var config = settings.Value;
        _defaultIndex = config.DefaultIndex;

        var settingsConfig = new ElasticsearchClientSettings(new Uri(config.Uri))
            .DefaultIndex(_defaultIndex);

        if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
        {
            settingsConfig.Authentication(new BasicAuthentication(config.Username, config.Password));
        }

        _client = new ElasticsearchClient(settingsConfig);
    }

    public async Task<bool> IndexArticleAsync(Article article, CancellationToken cancellationToken = default)
    {
        var response = await _client.IndexAsync(article, idx => idx.Index(_defaultIndex).Id(article.Id), cancellationToken);

        if (response.IsValidResponse)
        {
            _logger.LogInformation("Article {ArticleId} indexed successfully", article.Id);
            return true;
        }

        _logger.LogError("Failed to index article {ArticleId}: {Error}", article.Id, response.DebugInformation);
        return false;
    }

    public async Task<Article?> GetArticleByIdAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<Article>(articleId.ToString(), idx => idx.Index(_defaultIndex), cancellationToken);

        if (response.IsValidResponse && response.Found)
        {
            return response.Source;
        }

        return null;
    }

    public async Task<List<Article>> SearchArticlesAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        var response = await _client.SearchAsync<Article>(s => s
            .Indices(_defaultIndex)
            .Size(maxResults)
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(new[] { "title^2", "content" })
                    .Query(query)
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            ),
            cancellationToken
        );

        if (response.IsValidResponse)
        {
            return response.Documents.ToList();
        }

        _logger.LogError("Search failed: {Error}", response.DebugInformation);
        return new List<Article>();
    }
}
