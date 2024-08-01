using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy;

class HtmlExtractorManager
{
    public async Task ExtractDataFromHtmlAsync((string url, string content) urlContent, ScraperConfig? config)
    {
        await using var log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();

        HtmlSelectorExtractor selectorExtractor = new HtmlSelectorExtractor();
        var urlConfig = config?.Websites!.First(v => v.Domain == new Uri(urlContent.url).Host);

        try
        {
            var elements = new Dictionary<string, string>();

            foreach (var selector in urlConfig?.Selectors!)
            {
                var value = selectorExtractor.ParseWithSelector(urlContent.content, selector.Value);
                elements[selector.Key] = value;
            }

            var result = new ScrapedDataModel
            {
                Website = new Uri(urlContent.url).Host,
                Url = urlContent.url,
                Elements = elements,
                Created = DateTime.UtcNow
            };

            log.Information("Successfully scraped: {Url} - Elements: {@Elements}", result.Url, elements);

            await ScrapedDataStorage.OutputToSqlServer(result);
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
    }
}