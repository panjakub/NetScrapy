using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;
using System.Text.RegularExpressions;

namespace NetScrapy;

public class BatchManager
{
    private readonly Dictionary<string, Queue<string?>> _selectedUris;
    private readonly Dictionary<string, DateTime> _lastAccessPerDomain;
    private readonly BatchProcessor _batchProcessor;
    private readonly ScraperConfig? _scraperConfig;
    private readonly HtmlExtractorManager _extractor = new HtmlExtractorManager();
    private readonly Logger _log;
    private readonly RobotsHandler _robotsHandler;
    private Dictionary<string, List<string>> _discoveredUris = new();
    private List<string> _visitedUris = new();


    public BatchManager(Dictionary<string, Queue<string?>> selectedUris, ScraperConfig? config, BatchProcessor batchProcessor)
    {
        _selectedUris = selectedUris;
        _lastAccessPerDomain = selectedUris.Keys.ToDictionary(k => k, _ => DateTime.Now);
        _batchProcessor = batchProcessor;
        _scraperConfig = config;
        _robotsHandler = new RobotsHandler("*", _scraperConfig!);
        
        _log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();
        
    }

    private void HtmlExtractorOnActivityDetected(string url)
    {
        _lastAccessPerDomain[new Uri(url).Host] = DateTime.Now + TimeSpan.FromMinutes(5);
        _log.Warning($"Due to suspected detection at {new Uri(url).Host} adding 5 minute cooldown (affected URL: {url}");
        _selectedUris[new Uri(url).Host].Enqueue(url);
        _log.Warning($"Re-adding {url} at the back of the queue.");
    }

    private string GetBaseUrl(string url)
    {
        Uri uri = new Uri(url);
        return uri.GetLeftPart(UriPartial.Path);
    }


    public async Task RunAndSaveAsync()
    {
        _extractor.ActivityDetected += HtmlExtractorOnActivityDetected;

        while (_selectedUris.Any(d => d.Value.Count > 0))
        {
            Queue<string> batch = GetNextBatch();
            Dictionary<string, string> results = await _batchProcessor.GetBatchContentAsync(batch);

            foreach (var result in results)
            {
                var output = await _extractor.ExtractDataFromHtmlAsync((result.Key, result.Value), _scraperConfig);
                _visitedUris.Add(result.Key);
                
                foreach (var kvp in _extractor.ExtractLinks(result.Key, result.Value))
                {
                    _discoveredUris.TryAdd(kvp.Key, kvp.Value);
                }

                foreach (var discoveredUri in _discoveredUris.Values.First())
                {
                    var uriPartial = GetBaseUrl(discoveredUri);
                    var urlPattern = _scraperConfig!.Websites!
                                .First(d => d.AcceptHost != null && d.AcceptHost.Any(host => host == new Uri(result.Key).Host))
                                .ProductUrlPattern!;

                    //if (_selectedUris.ContainsKey(new Uri(uriPartial).Host)) continue;

                    if (
                        (await _robotsHandler.IsAllowed(uriPartial) & _selectedUris[_discoveredUris.Keys.First()].All(u => u != uriPartial))
                        && Regex.Match(uriPartial, urlPattern, RegexOptions.IgnoreCase).Success
                        && uriPartial != result.Key
                        && !(Regex.Match(uriPartial, @"\.(jpg|pdf|png|xml)", RegexOptions.IgnoreCase).Success)
                        && !(_discoveredUris.Any(k => k.Key.Contains(uriPartial)))
                        && !(_visitedUris.Contains(uriPartial))
                        )
                    {
                        _selectedUris[_discoveredUris.Keys.First()].Enqueue(uriPartial);
                        _log.Information($"Added {uriPartial} to queue for {result.Key}");
                    }
                }

                if (output!.Elements!.ContainsKey("Detected"))
                {
                    HtmlExtractorOnActivityDetected(output.Url!);
                }
                else
                {
                    await ScrapedDataStorage.OutputToSqlServer(output);
                }
                
            }
        }
    }

    private Queue<string> GetNextBatch()
    {
        Queue<string> batch = new Queue<string>();

        while (_selectedUris.Values.Any(queue => queue.Count > 0))
        {
            foreach (var domain in _selectedUris.Keys)
            {
                if (batch.Count < _selectedUris.Keys.Count)
                {
                    try
                    {
                        if (!CanAccessDomain(domain)) continue;
                        batch.Enqueue(_selectedUris[domain].Dequeue()!);
                        _lastAccessPerDomain[domain] = DateTime.Now;
                    }
                    catch (InvalidOperationException) {}
                }
                else
                {
                    break;
                }
            }
            break;
        }

        return batch;
    }

    bool CanAccessDomain(string domain)
    {
        var lastAccess = _lastAccessPerDomain[domain];
        var minDelaySeconds = _scraperConfig?.Websites!
            .Where(w => w.AcceptHost != null && w.AcceptHost.Any(host => host == domain))
            .Select(d => d.Timeout)
            .First();

        return (DateTime.Now - lastAccess).TotalMilliseconds >= minDelaySeconds;
    }
}