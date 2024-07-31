using Serilog;
using System.Net;
using Serilog.Sinks.SystemConsole.Themes;

public class HttpClientWrapper
{
    private readonly Dictionary<string, string> _defaultHeaders;

    public HttpClientWrapper(Dictionary<string, string>? defaultHeaders = null)
    {
        _defaultHeaders = defaultHeaders ?? new Dictionary<string, string>();

        AddHeaders(defaultHeaders!);
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

    public async Task<string> GetContentAsync(string url)
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

    //private async Task<string> DecompressIfNeeded(byte[] content)
    //{
    //    // Check if content is gzipped
    //    if (content.Length > 2 && content[0] == 0x1f && content[1] == 0x8b)
    //    {
    //        using (var compressedStream = new MemoryStream(content))
    //        using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
    //        using (var resultStream = new MemoryStream())
    //        {
    //            await zipStream.CopyToAsync(resultStream);
    //            return Encoding.UTF8.GetString(resultStream.ToArray());
    //        }
    //    }
    //    else
    //    {
    //        return Encoding.UTF8.GetString(content);
    //    }
    //}


    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.All /*DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.All*/
        };

        var client = new HttpClient(handler);

        foreach (var header in _defaultHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client;
    }
}