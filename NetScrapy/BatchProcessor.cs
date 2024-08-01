namespace NetScrapy;

public class BatchProcessor
{
    private readonly HttpClientWrapper _httpClient;
    private readonly ScraperConfig? _scraperConfig;

    public BatchProcessor(ScraperConfig? config)
    {
        _httpClient = new HttpClientWrapper();
        _scraperConfig = config;

        if (_scraperConfig?.DefaultHeaders != null)
        {
            _httpClient.AddHeaders(_scraperConfig.DefaultHeaders);
        }
    }

    public async Task<Dictionary<string, string>> GetBatchContentAsync(IEnumerable<string> items, bool usePw = false)
    {
        var taskCompletionSources = new Dictionary<string, TaskCompletionSource<string>>();
        var tasks = new Queue<Task>();

        foreach (var item in items)
        {
            var tcs = new TaskCompletionSource<string>();
            taskCompletionSources[item] = tcs;

            tasks.Enqueue(RetrieveContentAsync(item, tcs));
        }

        await Task.WhenAll(tasks);

        var results = new Dictionary<string, string>();
        foreach (var kvp in taskCompletionSources)
        {
            results[kvp.Key] = kvp.Value.Task.Result;
        }

        return results;
    }

    private async Task RetrieveContentAsync(string url, TaskCompletionSource<string> tcs)
    {
        var urlConfig = _scraperConfig?.Websites!.First(v => v.Domain == new Uri(url).Host);
        bool usePw = urlConfig is { IsJs: true } && !url.EndsWith(".xml");

        try
        {
            var responsePw = await _httpClient.GetContentAsync(url, usePw);
            tcs.SetResult(responsePw);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }
}