namespace HybridSearch.API.Events;

public record ArticleCreatedEvent
{
    public Guid ArticleId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
