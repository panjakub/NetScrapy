﻿using Serilog;
using System.Net;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.Playwright;
using Azure.Core;

public class HttpClientWrapper
{
    private readonly Dictionary<string, string> _defaultHeaders;
    private readonly Serilog.Core.Logger _log;

    public HttpClientWrapper(Dictionary<string, string>? defaultHeaders = null)
    {
        _defaultHeaders = defaultHeaders ?? new Dictionary<string, string>();

        AddHeaders(defaultHeaders!);

        using Serilog.Core.Logger log = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();

        _log = log;
    }

    public virtual void AddHeaders(Dictionary<string, string> headers)
    {
        if (headers != null)
        {
            foreach (var header in headers)
            {
                _defaultHeaders[header.Key] = header.Value;
            }
        }
    }

    public async Task<string> GetContentAsync(string url, bool usePw = false)
    {
        if (usePw)
        {
            var page = await CreatePlaywrightClient();
            try
            {
                _log.Information($"Awaiting page content for {url}");
                await page.GotoAsync(url, new() { Timeout = 100000 });
                var content = await page.ContentAsync();
                _log.Information($"Content retrieved for {url}, response length: {content.Length}");
                return content;
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        else
        {
            using (var httpClient = CreateHttpClient())
            {
                var response = await httpClient.GetAsync(url);
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
        _log.Information("Launching headless browser");
        var browser = await chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetExtraHTTPHeadersAsync(_defaultHeaders);

        return page;
    }
}