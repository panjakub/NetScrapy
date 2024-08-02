using System.IO.Compression;
using System.Net;
using Microsoft.Playwright;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy;

public sealed class HttpClientWrapper
{
    private readonly Dictionary<string, string> _defaultHeaders;
    private readonly Serilog.Core.Logger _log;

    public HttpClientWrapper(Dictionary<string, string>? defaultHeaders = null)
    {
        _defaultHeaders = defaultHeaders ?? new Dictionary<string, string>();

        if (defaultHeaders != null) AddHeaders(defaultHeaders);

        using Serilog.Core.Logger log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();

        _log = log;
    }

    public void AddHeaders(Dictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            _defaultHeaders[header.Key] = header.Value;
        }
    }

    public async Task<string> GetContentAsync(string url, bool usePw = false)
    {
        if (usePw)
        {
            var page = await CreatePlaywrightClient();
            string content = string.Empty;
            
            try
            {
                _log.Information($"Awaiting page content for {url}");
                await page.GotoAsync(url, new PageGotoOptions { Timeout = 100000 });
                page.RequestFinished += async (_, response) =>
                {
                    if (response.Failure != null)
                    {
                        content = await page.ContentAsync();
                        _log.Information($"Content retrieved for {url}, response length: {content.Length}");
                    }
                    else
                    {
                        _log.Error($"Error accessing {url}: {response.Failure}");
                        content = String.Empty;
                    }
                };
                return content;
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        else
        {
            using var httpClient = CreateHttpClient();
            var response = await httpClient.GetAsync(url);
            if (url.Contains("gz"))
            {
                using (var compressed = await response.Content.ReadAsStreamAsync())
                {
                    using (var decompressed = new GZipStream(compressed, CompressionMode.Decompress))
                    {
                        using (var reader = new StreamReader(decompressed))
                        {
                            return await reader.ReadToEndAsync();
                        }
                    }
                }
            }
            try
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                await Task.Run(() => Log.Error($"{url} returned {ex.StatusCode})"));
                return await Task.Run(() => string.Empty);
            }
        }
    }


    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.All
        };

        var client = new HttpClient(handler);

        foreach (var header in _defaultHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client;
    }


    private async Task<IPage> CreatePlaywrightClient() 
    {
        var client = await Playwright.CreateAsync();

        var chromium = client.Chromium;
        var browser = await chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetExtraHTTPHeadersAsync(_defaultHeaders);

        return page;
    }
}