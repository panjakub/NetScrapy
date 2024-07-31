public class ScrapedDataModel
{
    public int Id { get; set; }
    public string? Website { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, string>? Elements { get; set; }
    public DateTime? Created { get; set; }
}