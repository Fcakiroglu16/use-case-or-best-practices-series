using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using HybridSearch.API.Configuration;
using HybridSearch.API.Models;
using Microsoft.Extensions.Options;

namespace HybridSearch.API.Extensions;

public static class ElasticsearchExtensions
{
    public static async Task CreateArticleIndexAsync(this ElasticsearchClient client, string indexName)
    {
        Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse existsResponse = await client.Indices.ExistsAsync(indexName);

        if (existsResponse.Exists)
        {
            return;
        }

        CreateIndexResponse createIndexResponse = await client.Indices.CreateAsync(indexName, c => c
            .Mappings(m => m
                .Properties<Article>(p => p
                    .Keyword(k => k.Id)
                    .Text(t => t.Title, td => td
                        .Fields(f => f
                            .Keyword("keyword")))
                    .Text(t => t.Content, td => td
                        .Fields(f => f
                            .Keyword("keyword")))
                    .Date(d => d.CreatedAt)
                    .Date(d => d.UpdatedAt)))
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)));

        if (!createIndexResponse.IsValidResponse)
        {
            throw new Exception($"Failed to create index: {createIndexResponse.ElasticsearchServerError?.Error?.Reason}");
        }
    }

    public static async Task InitializeElasticsearchAsync(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ElasticsearchClient client = scope.ServiceProvider.GetRequiredService<ElasticsearchClient>();
        ElasticsearchSettings settings = scope.ServiceProvider.GetRequiredService<IOptions<ElasticsearchSettings>>().Value;

        await client.CreateArticleIndexAsync(settings.DefaultIndex);
    }
}