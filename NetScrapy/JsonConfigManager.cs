using System.Text.Json;

namespace NetScrapy;

public class JsonConfigManager
{
    public static ScraperConfig? LoadConfig(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        return JsonSerializer.Deserialize<ScraperConfig>(json, options);
    }
}