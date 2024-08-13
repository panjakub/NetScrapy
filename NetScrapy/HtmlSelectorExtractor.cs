namespace NetScrapy;

public static class HtmlSelectorExtractor
{
    public static string ParseWithSelector(string html, string selector)
    {
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(html);
        var node = htmlDoc.DocumentNode.SelectSingleNode(selector);

        if (node == null)
        {
            return string.Empty;
        }

        if (selector.Split("/").Last().StartsWith("@"))
        {
            var attributeName = selector.Split("/").Last().Replace("@", String.Empty);
            return node.GetAttributeValue(attributeName, string.Empty).Trim();
        }
        else
        {
            return node.InnerText.Trim();
        }
    }

    public static IEnumerable<string> ExtractLinks(string html, string baseUrl)
    {
        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(html);

        var links = new List<string>();
        var nodes = htmlDoc.DocumentNode.SelectNodes("//a[@href]");

        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                string href = node.GetAttributeValue("href", "");
                if (!string.IsNullOrEmpty(href))
                {
                    links.Add(new Uri(new Uri(baseUrl), href).AbsoluteUri);
                }
            }
        }

        return links;
    }
}