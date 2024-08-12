using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy;

class HtmlExtractorManager
{
    private ScrapedDataModel? _result;

    public event Action<string>?  ActivityDetected;  
    
    public async Task<ScrapedDataModel?> ExtractDataFromHtmlAsync((string url, string content) urlContent, ScraperConfig? config)
    {
        await using var log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();

        var (url, content) = urlContent;
        var urlConfig = config?.Websites!.First(v => v.AcceptHost != null && v.AcceptHost.Any(host => host == new Uri(url).Host));

        try
        {
            var elements = new Dictionary<string, string>();

            foreach (var selector in urlConfig?.Selectors!)
            {
                var value = HtmlSelectorExtractor.ParseWithSelector(content, selector.Value);
                elements[selector.Key] = value;

                if (!elements.All(pair => pair.Value.IsNullOrEmpty())) continue;
                elements.Add("Detected", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                ActivityDetected?.Invoke(url);
                log.Warning($"Activity detected for {url}");
            }

            _result = new ScrapedDataModel
            {
                Website = new Uri(url).Host,
                Url = url,
                Elements = elements,
                Created = DateTime.UtcNow,
                HtmlSnapshot = content
            };

            if (!_result.Elements.ContainsKey("Detected"))
            {
                log.Information("Successfully scraped Elements: {@Elements} from {Url} ", elements, _result.Url);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex.Source!);
            log.Error(ex.Message);
            log.Error(ex.InnerException!.ToString());
            log.Error(ex.StackTrace!);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
        return _result;
    }
}