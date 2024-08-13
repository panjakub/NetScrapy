using System.Xml.Linq;
using Serilog;

namespace NetScrapy;

public class SitemapParser
{
    private readonly ScraperConfig _config;

    public SitemapParser(ScraperConfig config)
    {
        _config = config;
    }

    public async Task<Dictionary<string, Queue<string?>>> BatchParseSitemapsAsync(Dictionary<string, string> data)
    {
        var processingTasks = data.Select(async kvp =>
        {
            try
            {
                return new KeyValuePair<string, Queue<string?>>(
                    kvp.Key, await ParseSingleSitemapAsync(kvp.Key, kvp.Value));
            }
            catch (Exception)
            {
                return new KeyValuePair<string, Queue<string?>>(kvp.Key, new Queue<string?>());
            }
        });

        var results = await Task.WhenAll(processingTasks);
        return new Dictionary<string, Queue<string?>>(results);
    }


    private async Task<Queue<string?>> ParseSingleSitemapAsync(string url, string sitemapContent)
    {
        if (AssumeMarkup(sitemapContent))
        {
            return await ParseSitemapXml(url, sitemapContent);
        }
        else
        {
            return ParseSitemapText(sitemapContent)!;
        }
    }

    private bool AssumeMarkup(string content)
    {
        return content.TrimStart().StartsWith("<");
    }

    private async Task<Queue<string?>> ParseSitemapXml(string url, string sitemapContent)
    {
        var nsOverride = _config?.Websites!
            .Where(w => w.AcceptHost != null && w.AcceptHost.Any(host => host == new Uri(url).Host))
            .Select(d => d.SitemapNamespace)
            .First() ?? "http://www.sitemaps.org/schemas/sitemap/0.9";

        try
        {
            XNamespace ns = nsOverride;
            XDocument doc = XDocument.Parse(sitemapContent);

            var urls = new Queue<string?>(doc.Descendants(ns + "url")
                .Select(u => u.Element(ns + "loc")?.Value)
                .Where(uri => !string.IsNullOrEmpty(uri)));

            var nestedSitemaps = new Queue<string?>(doc.Descendants(ns + "sitemap")
                .Select(s => s.Element(ns + "loc")?.Value)
                .Where(uri => !string.IsNullOrEmpty(uri)));

            foreach (var nestedSitemapUrl in nestedSitemaps)
            {
                var nestedTemp = await ParseSingleSitemapAsync(url, sitemapContent: nestedSitemapUrl!);
                urls.Enqueue(nestedTemp.Dequeue());
            }

            return await Task.Run(() => urls);
        }
        catch (Exception ex)
        {
            Log.Error($"Error parsing XML sitemap: {ex.Message}");
            return await Task.Run(() => new Queue<string?>());
        }
    }

    private Queue<string> ParseSitemapText(string sitemapContent)
    {
        return new Queue<string>(sitemapContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)));
    }
}