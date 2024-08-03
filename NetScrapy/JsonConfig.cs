using System.Text.Json.Serialization;

namespace NetScrapy;

public class ScraperConfig
{
    [JsonPropertyName("defaultHeaders")]
    public Dictionary<string, string>? DefaultHeaders { get; set; }

    [JsonPropertyName("globalSettings")]
    public GlobalSettings? GlobalSettings { get; set; }

    [JsonPropertyName("websites")]
    public List<WebsiteConfig>? Websites { get; set; }
}

public class GlobalSettings
{
    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; }

    [JsonPropertyName("maxConcurrency")]
    public int MaxConcurrency { get; set; }
}

public class WebsiteConfig
{
    [JsonPropertyName("acceptHost")]
    public List<string?>? AcceptHost { get; set; }

    [JsonPropertyName("sitemapUrls")] 
    public List<string?> SitemapUrls { get; set; } = new();

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; }

    [JsonPropertyName("isJS")]
    public bool IsJs { get; set; }

    [JsonPropertyName("selectors")]
    public Dictionary<string, string>? Selectors { get; set; } = new Dictionary<string, string>();

    [JsonPropertyName("productUrlPattern")]
    public string? ProductUrlPattern { get; set; }

    [JsonPropertyName("excludeUrlPattern")]
    public string? ExcludeUrlPattern { get; set; }
}