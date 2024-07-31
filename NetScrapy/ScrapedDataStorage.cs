using Microsoft.EntityFrameworkCore;

public class ScrapedDataStorage
{
    public static Func<ScrapedDataModel, Task> outputToSqlServer = async (scrapedData) =>
    {
        using (var context = new ScrapedDataContext())
        {
            // Check if the entry already exists
            var existingEntry = await context.ScrapedData
                .FirstOrDefaultAsync(sd => sd.Url == scrapedData.Url);

            if (existingEntry != null)
            {
                // Update existing entry
                existingEntry.Website = scrapedData.Website;
                existingEntry.Elements = scrapedData.Elements;
                existingEntry.Created = scrapedData.Created;
                context.ScrapedData.Update(existingEntry);
            }
            else
            {
                // Add new entry
                await context.ScrapedData.AddAsync(scrapedData);
            }

            await context.SaveChangesAsync();
        }
    };
}