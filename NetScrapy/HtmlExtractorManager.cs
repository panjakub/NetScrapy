using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

class HtmlExtractorManager
{
    public async Task ExtractDataFromHtmlAsync((string url, string content) urlContent, ScraperConfig config)
    {
        using var log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();

        HtmlSelectorExtractor _selectorExtractor = new HtmlSelectorExtractor();
        var urlConfig = config.Websites!.Where(v => v.Domain == new Uri(urlContent.url).Host).First();

        try
        {
            var elements = new Dictionary<string, string>();

            foreach (var selector in urlConfig.Selectors!)
            {
                var value = _selectorExtractor.ParseWithSelector(urlContent.content, selector.Value);
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

            await ScrapedDataStorage.outputToSqlServer(result);
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
            Log.CloseAndFlush();
        }
    }
}
