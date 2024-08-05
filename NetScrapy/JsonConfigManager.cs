using System.Text.Json;

namespace NetScrapy;

public class JsonConfigManager
{
    public static ScraperConfig? LoadConfig(string? filePath)
    {
        if (filePath != null)
        {
            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            return JsonSerializer.Deserialize<ScraperConfig>(json, options);
        }
        else
        {
            throw FileNotFoundException;
        }
    }

    private static Exception FileNotFoundException { get; set; } = new FileNotFoundException();
}