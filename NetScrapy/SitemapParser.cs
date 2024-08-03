using System.Xml.Linq;
using Serilog;

namespace NetScrapy;

public class SitemapParser
{
    public async Task<Dictionary<string, Queue<string?>>> BatchParseSitemapsAsync(Dictionary<string, string> data)
    {
        var processingTasks = data.Select(async kvp =>
        {
            try
            {
                return new KeyValuePair<string, Queue<string?>>(
                    kvp.Key, await ParseSingleSitemapAsync(kvp.Value));
            }
            catch (Exception)
            {
                return new KeyValuePair<string, Queue<string?>>(kvp.Key, new Queue<string?>());
            }
        });

        var results = await Task.WhenAll(processingTasks);
        return new Dictionary<string, Queue<string?>>(results);
    }


    private async Task<Queue<string?>> ParseSingleSitemapAsync(string sitemapContent)
    {
        if (AssumeMarkup(sitemapContent))
        {
            return await ParseSitemapXml(sitemapContent);
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

    private async Task<Queue<string?>> ParseSitemapXml(string sitemapContent)
    {
        try
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XDocument doc = XDocument.Parse(sitemapContent);

            var urls = new Queue<string?>(doc.Descendants(ns + "url")
                .Select(u => u.Element(ns + "loc")?.Value)
                .Where(url => !string.IsNullOrEmpty(url)));

                var nestedSitemaps = new Queue<string?>(doc.Descendants(ns + "sitemap")
                    .Select(s => s.Element(ns + "loc")?.Value)
                    .Where(url => !string.IsNullOrEmpty(url)));

            foreach (var nestedSitemapUrl in nestedSitemaps)
            {
                var nestedTemp = await ParseSingleSitemapAsync(sitemapContent: nestedSitemapUrl!);
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