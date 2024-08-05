using Microsoft.EntityFrameworkCore;

namespace NetScrapy;

public static class ScrapedDataStorage
{
    public static readonly Func<ScrapedDataModel, Task> OutputToSqlServer = async (scrapedData) =>
    {
        await using var context = new ScrapedDataContext();
        // Check if the entry already exists
        var existingEntry = await context.ScrapedData
            .FirstOrDefaultAsync(sd => sd.Url == scrapedData.Url && sd.Created == scrapedData.Created);

        if (existingEntry != null)
        {
            // Update existing entry
            existingEntry.Website = scrapedData.Website;
            existingEntry.Elements = scrapedData.Elements;
            existingEntry.Created = scrapedData.Created;
            existingEntry.HtmlSnapshot = scrapedData.HtmlSnapshot;
            context.ScrapedData.Update(existingEntry);
        }
        else
        {
            // Add new entry
            await context.ScrapedData.AddAsync(scrapedData);
        }

        await context.SaveChangesAsync();
    };
}