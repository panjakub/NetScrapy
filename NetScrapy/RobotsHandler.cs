using System.Collections.Concurrent;
using System.Net;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace NetScrapy
{
    public class RobotsHandler
    {
        private readonly HttpClientWrapper _httpClient;
        private readonly ScraperConfig? _scraperConfig;
        private readonly string _userAgent;
        private readonly Logger _log;
        private Dictionary<string, List<string>> _robotsCache = new Dictionary<string, List<string>>();


        public RobotsHandler(string userAgent, ScraperConfig config)
        {
            _log = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            _userAgent = userAgent;
            _scraperConfig = config;

            _httpClient = new HttpClientWrapper();

            if (_scraperConfig?.DefaultHeaders != null)
            {
                _httpClient.AddHeaders(_scraperConfig.DefaultHeaders);
            }
        }

        public async Task<bool> IsAllowed(string url)
        {
            string domain = new Uri(url).Host;
            if (!string.IsNullOrEmpty(domain))
            {
                {
                    List<string> disallowedRules;

                    if (!_robotsCache.TryGetValue(domain, out disallowedRules!))
                    {
                        disallowedRules = await DownloadAndParseRobotsTxt(url);
                        _robotsCache[domain] = disallowedRules;
                    }
                    return !disallowedRules.Any(rule => url.StartsWith(rule));
                }
            }

            return false;

        }

        private async Task<List<string>> DownloadAndParseRobotsTxt(string url)
        {
            string? robotsUrl = _scraperConfig?.Websites!
            .Where(w => w.AcceptHost != null && w.AcceptHost.Any(host => host == new Uri(url).Host))
            .Select(d => d.RobotsFile)
            .DefaultIfEmpty(null).First();

            List<string> disallowedRules = new List<string>();

            if (robotsUrl != null)
            {
                string robotsContent = await _httpClient.GetContentAsync(robotsUrl);
                using (StringReader reader = new StringReader(robotsContent))
                {
                    string line;
                    string currentUserAgent = null!;

                    while ((line = reader.ReadLine()!) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("User-agent:"))
                        {
                            currentUserAgent = line.Substring("User-agent:".Length).Trim();
                        }
                        else if (line.StartsWith("Disallow:") && (currentUserAgent == "*" || currentUserAgent == _userAgent))
                        {
                            string rule = line.Substring("Disallow:".Length).Trim();
                            if (!string.IsNullOrEmpty(rule))
                            {
                                disallowedRules.Add(rule);
                            }
                        }
                    }
                }
            }

            return disallowedRules;
        }
    }

} 
