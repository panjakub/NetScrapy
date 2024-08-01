﻿using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy
{
    internal static class Program
    {
        static async Task Main(string?[] args)
        {
            ScraperConfig? config;
            var configManager = new JsonConfigManager();

            await using var log = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            CommandLineParser argParser = new CommandLineParser(args);

            try
            {
                if (argParser.Arg.TryGetValue("config", out var jsonConfig))
                {
                    log.Information($"Using {jsonConfig}");
                    config = configManager.LoadConfig(jsonConfig);
                }
                else { return; }
            }
            catch (NullReferenceException)
            {
                argParser.PrintHelp();
                return;
            }

            var startTime = DateTime.Now;
            log.Information($"Started at {startTime}");
            
            
            BatchProcessor batchProcessor = new BatchProcessor(config!);
            SitemapParser sitemapParser = new SitemapParser();


            var sitemapUris = config!.Websites!.Select(w => w.SitemapUrl);

            foreach (var website in config.Websites!)
            {
                log.Debug($"Loaded config for {website.Domain}:");
                log.Debug($"  Sitemap URL: {website.SitemapUrl}");
                log.Debug($"  Timeout: {website.Timeout}");
                log.Debug($"  Product URL Pattern: {website.ProductUrlPattern}");
                log.Debug($"  Selectors:");
                if (website.Selectors != null)
                {
                    foreach (var selector in website.Selectors)
                    {
                        log.Debug($"    {selector.Key}: {selector.Value}");
                    }
                }
                else
                {
                    log.Warning("    No selectors found");
                }
            }

            Dictionary<string, string> rawSitemaps = await batchProcessor.GetBatchContentAsync(sitemapUris!);

            Dictionary<string, Queue<string?>> urisToParse = sitemapParser.BatchParseSitemapsAsync(rawSitemaps).Result.ToDictionary(
                pair => new Uri(pair.Key).Host,
                pair => new Queue<string?>(pair.Value
                        .Where(m => m!.Contains(config.Websites!
                            .First(d => d.Domain! == new Uri(pair.Key).Host)
                            .ProductUrlPattern!)
                        )
                    )
                );


            int totalItems = 0;
            foreach (var item in urisToParse)
            {
                log.Information($"For {item.Key} : {item.Value.Count} items found.");
                totalItems += item.Value.Count;
            }

            log.Information($"{totalItems} elements to scrape.");

            BatchManager scraper = new BatchManager(urisToParse, config, batchProcessor);
            await scraper.RunAndSaveAsync();

            var finishTime = DateTime.Now;
            log.Information($"Finished at {finishTime}");

            var totalTime = (finishTime - startTime).TotalSeconds;
            var avgProcessingTime = totalItems / totalTime;

            log.Information($"Processing speed: {avgProcessingTime}");

            await Log.CloseAndFlushAsync();
        }
    }
}
