using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HybridSearch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(
        IElasticsearchService elasticsearchService,
        ILogger<ArticlesController> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new article and index it in Elasticsearch
    /// </summary>
    /// <param name="request">Article creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created article</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Article), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Article>> CreateArticle(
        [FromBody] CreateArticleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            var indexed = await _elasticsearchService.IndexArticleAsync(article, cancellationToken);

            if (!indexed)
            {
                _logger.LogError("Failed to index article {ArticleId}", article.Id);
                return StatusCode(500, new { error = "Failed to index article in Elasticsearch" });
            }

            _logger.LogInformation("Article {ArticleId} created successfully", article.Id);

            return CreatedAtAction(
                nameof(GetArticle),
                new { id = article.Id },
                article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article");
            return StatusCode(500, new { error = "An error occurred while creating the article" });
        }
    }

    /// <summary>
    /// Get article by ID
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Article details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Article), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Article>> GetArticle(Guid id, CancellationToken cancellationToken)
    {
        var article = await _elasticsearchService.GetArticleByIdAsync(id, cancellationToken);

        if (article == null)
        {
            return NotFound(new { error = $"Article with ID {id} not found" });
        }

        return Ok(article);
    }

    /// <summary>
    /// Search articles by query
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching articles</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Article>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Article>>> SearchArticles(
        [FromQuery] string q,
        [FromQuery] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Query parameter 'q' is required" });
        }

        var articles = await _elasticsearchService.SearchArticlesAsync(q, maxResults, cancellationToken);

        return Ok(articles);
    }
}
