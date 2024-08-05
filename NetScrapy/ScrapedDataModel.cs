namespace NetScrapy;

public class ScrapedDataModel
{
    public int Id { get; init; }
    public string? Website { get; set; }
    public string? Url { get; init; }
    public Dictionary<string, string>? Elements { get; set; }
    public DateTime? Created { get; set; }
    public string? HtmlSnapshot { get; set; }
}