namespace HybridSearch.API.Configuration;

public class ElasticsearchSettings
{
    public string Uri { get; set; } = "http://localhost:9200";
    public string DefaultIndex { get; set; } = "articles";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
