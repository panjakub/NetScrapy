public class HtmlSelectorExtractor
{
    public string ParseWithSelector(string html, string selector)
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
            string attributeName = selector.Split("/").Last().Replace("@", String.Empty);
            return node.GetAttributeValue(attributeName, string.Empty).Trim();
        }
        else
        {
            return node.InnerText.Trim();
        }
    }
}