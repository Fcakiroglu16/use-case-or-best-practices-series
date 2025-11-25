using HybridSearch.API.Models;
using HybridSearch.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HybridSearch.API.Endpoints;

public static class ArticlesEndpoints
{
    public static void MapArticlesEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/articles")
            .WithTags("Articles");

        group.MapPost("/", CreateArticle)
            .WithName("CreateArticle")
            .Produces<Article>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}", GetArticle)
            .WithName("GetArticle")
            .Produces<Article>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/search", SearchArticles)
            .WithName("SearchArticles")
            .Produces<List<Article>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateArticle(
        [FromBody] CreateArticleRequest request,
        IElasticsearchService elasticsearchService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        var indexed = await elasticsearchService.IndexArticleAsync(article, cancellationToken);

        if (!indexed)
        {
            logger.LogError("Failed to index article {ArticleId}", article.Id);
            return Results.Problem(
                "Failed to index article in Elasticsearch",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        logger.LogInformation("Article {ArticleId} created successfully", article.Id);

        return Results.CreatedAtRoute("GetArticle", new { id = article.Id }, article);
    }

    private static async Task<IResult> GetArticle(
        Guid id,
        IElasticsearchService elasticsearchService,
        CancellationToken cancellationToken)
    {
        var article = await elasticsearchService.GetArticleByIdAsync(id, cancellationToken);

        if (article == null)
        {
            return Results.NotFound(new { error = $"Article with ID {id} not found" });
        }

        return Results.Ok(article);
    }

    private static async Task<IResult> SearchArticles(
        [FromQuery] string q,
        [FromQuery] int maxResults,
        IElasticsearchService elasticsearchService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest(new { error = "Query parameter 'q' is required" });
        }

        var articles = await elasticsearchService.SearchArticlesAsync(q, maxResults, cancellationToken);

        return Results.Ok(articles);
    }
}
