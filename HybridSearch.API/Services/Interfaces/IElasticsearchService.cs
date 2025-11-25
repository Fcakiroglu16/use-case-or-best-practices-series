using HybridSearch.API.Models;

namespace HybridSearch.API.Services.Interfaces;

public interface IElasticsearchService
{
    Task<bool> IndexArticleAsync(Article article, CancellationToken cancellationToken = default);
    Task<Article?> GetArticleByIdAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<List<Article>> SearchArticlesAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default);
}
