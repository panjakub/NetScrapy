using System.Collections.Concurrent;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy;

public class BatchManager
{
    private readonly Dictionary<string, Queue<string?>> _selectedUris;
    private readonly Dictionary<string, DateTime> _lastAccessPerDomain;
    private readonly BatchProcessor _batchProcessor;
    private readonly ScraperConfig? _scraperConfig;
    private readonly HtmlExtractorManager _extractor;
    private readonly Logger log;

    public BatchManager(Dictionary<string, Queue<string?>> selectedUris, ScraperConfig? config, BatchProcessor batchProcessor)
    {
        _selectedUris = selectedUris;
        _lastAccessPerDomain = selectedUris.Keys.ToDictionary(k => k, _ => DateTime.Now);
        _batchProcessor = batchProcessor;
        _scraperConfig = config;
        _extractor = new HtmlExtractorManager();
        
        log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();
        
    }

    private void HtmlExtractorOnActivityDetected(string url)
    {
        _lastAccessPerDomain[new Uri(url).Host] = DateTime.Now + TimeSpan.FromMinutes(5);
        log.Warning($"Due to suspected detection at {new Uri(url).Host} adding 5 minute cooldown (affected URL: {url}");
        _selectedUris[new Uri(url).Host].Enqueue(url);
        log.Warning($"Re-adding {url} at the back of the queue.");
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

                if (output.Elements.ContainsKey("Detected"))
                {
                    HtmlExtractorOnActivityDetected(output.Url);
                }
                else if (output != null)
                {
                    await ScrapedDataStorage.OutputToSqlServer(output);
                }
                
            }
        }
    }

    private Queue<string> GetNextBatch()
    {
        Queue<string> batch = new Queue<string>();

        // var lastAccessPerDomain = _selectedUris.Keys.ToDictionary(k => k, _ => DateTime.Now);

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
                    catch (InvalidOperationException)
                    {
                        _selectedUris.Remove(domain);
                    }
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