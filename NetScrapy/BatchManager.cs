namespace NetScrapy;

public class BatchManager
{
    private readonly Dictionary<string, Queue<string?>> _selectedUris;
    private readonly Dictionary<string, DateTime> _lastAccessPerDomain;
    private readonly BatchProcessor _batchProcessor;
    private readonly ScraperConfig? _scraperConfig;
    private readonly HtmlExtractorManager _extractor;

    public BatchManager(Dictionary<string, Queue<string?>> selectedUris, ScraperConfig? config, BatchProcessor batchProcessor)
    {
        _selectedUris = selectedUris;
        _lastAccessPerDomain = selectedUris.Keys.ToDictionary(k => k, _ => DateTime.Now);
        _batchProcessor = batchProcessor;
        _scraperConfig = config;
        _extractor = new HtmlExtractorManager();
    }

    public async Task RunAndSaveAsync()
    {
        while (_selectedUris.Any(d => d.Value.Count > 0))
        {
            Queue<string> batch = GetNextBatch();
            Dictionary<string, string> results = await _batchProcessor.GetBatchContentAsync(batch);

            foreach (var result in results)
            {
                await _extractor.ExtractDataFromHtmlAsync((result.Key, result.Value), _scraperConfig);
            }
        }
    }

    private Queue<string> GetNextBatch()
    {
        Queue<string> batch = new Queue<string>();

        var lastAccessPerDomain = _selectedUris.Keys.ToDictionary(k => k, _ => DateTime.Now);

        while (_selectedUris.Values.Any(queue => queue.Count > 0))
        {
            foreach (var domain in _selectedUris.Keys)
            {
                if (batch.Count < _selectedUris.Keys.Count && CanAccessDomain(domain))
                {
                    try
                    {
                        batch.Enqueue(_selectedUris[domain].Dequeue()!);
                        lastAccessPerDomain[domain] = DateTime.Now;
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
            .Where(w => w.Domain == domain)
            .Select(d => d.Timeout)
            .First();

        return (DateTime.Now - lastAccess).TotalMilliseconds >= minDelaySeconds;
    }
}