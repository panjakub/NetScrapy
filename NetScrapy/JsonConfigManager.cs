using System.Text.Json;

public class JsonConfigManager
{
    public ScraperConfig? LoadConfig(string filePath)
    {
        string json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        return JsonSerializer.Deserialize<ScraperConfig>(json, options);
    }
}