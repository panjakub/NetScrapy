public class BatchProcessor
{
    private readonly HttpClientWrapper _httpClient;
    private readonly ScraperConfig _scraperConfig;

    public BatchProcessor(ScraperConfig config)
    {
        _httpClient = new HttpClientWrapper();
        _scraperConfig = config;

        if (_scraperConfig.DefaultHeaders != null)
        {
            _httpClient.AddHeaders(_scraperConfig.DefaultHeaders);
        }
    }

    public async Task<Dictionary<string, string>> GetBatchContentAsync(IEnumerable<string> items)
    {
        var taskCompletionSources = new Dictionary<string, TaskCompletionSource<string>>();
        var tasks = new List<Task>();

        foreach (var item in items)
        {
            var tcs = new TaskCompletionSource<string>();
            taskCompletionSources[item] = tcs;

            tasks.Add(RetrieveContentAsync(item, tcs));
        }

        await Task.WhenAll(tasks);

        var results = new Dictionary<string, string>();
        foreach (var kvp in taskCompletionSources)
        {
            results[kvp.Key] = kvp.Value.Task.Result;
        }

        return results;
    }

    private async Task RetrieveContentAsync(string item, TaskCompletionSource<string> tcs)
    {
        try
        {
            var response = await _httpClient.GetContentAsync(item);
            tcs.SetResult(response);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }
}
