using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy;

class HtmlExtractorManager
{
    private ScrapedDataModel? _result;
    private Logger _log;

    public event Action<string>?  ActivityDetected;

    public HtmlExtractorManager()
    {
        _log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();
    }

    public async Task<ScrapedDataModel?> ExtractDataFromHtmlAsync((string url, string content) urlContent, ScraperConfig? config)
    {
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
                _log.Warning($"Activity detected for {url}");
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
                _log.Information("Successfully scraped Elements: {@Elements} from {Url} ", elements, _result.Url);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex.Source!);
            _log.Error(ex.Message);
            _log.Error(ex.InnerException!.ToString());
            _log.Error(ex.StackTrace!);
        }

        return _result;
    }

    public Dictionary<string, List<string>> ExtractLinks(string url, string content)
    {
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(content);

        var domain = new Uri(url).Host;

        var links = new List<string>();
        var nodes = htmlDoc.DocumentNode.SelectNodes("//a[@href]");

        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                string href = node.GetAttributeValue("href", "");
                if (!string.IsNullOrEmpty(href))
                {
                    links.Add(new Uri(new Uri(url), href).AbsoluteUri);
                }
            }
        }

        var output = new Dictionary<string, List<string>>() { { domain, links } };
        _log.Information($"Discovered {links.Count} new urls.");


        return output;
    }
}