using System.Collections.Concurrent;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy
{
    public class RobotsHandler
    {
        private readonly HttpClientWrapper _httpClient;
        private readonly ScraperConfig? _scraperConfig;
        private readonly ConcurrentDictionary<string, List<string>> _disallowedPaths;
        private readonly ConcurrentDictionary<string, Task> _fetchTasks;
        private readonly string _userAgent;
        private readonly Logger _log;

        public RobotsHandler(string userAgent, ScraperConfig config, Logger logger)
        {
            _log = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            _disallowedPaths = new ConcurrentDictionary<string, List<string>>();
            _fetchTasks = new ConcurrentDictionary<string, Task>();
            _userAgent = userAgent;
            _scraperConfig = config;

            try
            {
                _httpClient = new HttpClientWrapper();
            }
            catch (Exception ex)
            {
                throw;
            }

            if (_scraperConfig?.DefaultHeaders != null)
            {
                try
                {
                    _httpClient.AddHeaders(_scraperConfig.DefaultHeaders);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public Task FetchRobotsTxtIfNeeded(string domain)
        {
            return _fetchTasks.GetOrAdd(domain, FetchRobotsTxtInternal);
        }

        private async Task FetchRobotsTxtInternal(string domain)
        {
            try
            {
                var robotsUrl = $"https://{domain}/robots.txt";
                var content = await _httpClient.GetContentAsync(robotsUrl);
                ParseRobotsTxt(domain, content);
            }
            catch (HttpRequestException ex)
            {
                _disallowedPaths[domain] = new List<string>();
            }
        }

        private void ParseRobotsTxt(string domain, string content)
        {
            var lines = content.Split('\n');
            bool relevantUserAgent = false;
            var disallowedPaths = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
                {
                    var agent = trimmedLine.Substring("User-agent:".Length).Trim();
                    relevantUserAgent = agent == "*" || agent.Equals(_userAgent, StringComparison.OrdinalIgnoreCase);
                }
                else if (relevantUserAgent && trimmedLine.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
                {
                    var path = trimmedLine.Substring("Disallow:".Length).Trim();
                    if (!string.IsNullOrEmpty(path))
                    {
                        disallowedPaths.Add(path);
                    }
                }
            }

            _disallowedPaths[domain] = disallowedPaths;
        }

        public bool IsAllowed(string url)
        {
            var uri = new Uri(url);
            var domain = uri.Host;
            var path = uri.PathAndQuery;

            if (_disallowedPaths.TryGetValue(domain, out var disallowedPaths))
            {
                foreach (var disallowedPath in disallowedPaths)
                {
                    if (path.StartsWith(disallowedPath))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}