using System.ComponentModel.DataAnnotations;

namespace HybridSearch.API.Models;

public class CreateArticleRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 500 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    [StringLength(10000, MinimumLength = 50, ErrorMessage = "Content must be between 50 and 10000 characters")]
    public string Content { get; set; } = string.Empty;
}
