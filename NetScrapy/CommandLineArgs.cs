using Serilog;

namespace NetScrapy;

class CommandLineParser
{
    public Dictionary<string, string?> Arg { get; set; }

    public CommandLineParser(string?[] args)
    {
        if (!args.Any() || args.Contains("--help"))
        {
            Log.Fatal("Configuration file is required.");
            PrintHelp();
        }
        else
        {
            Arg = ParseArguments(args);
        }
    }

    private Dictionary<string, string?> ParseArguments(string?[] args)
    {
        var arguments = new Dictionary<string, string?>();

        foreach (var arg in args)
        {
            string?[] parts = arg.Split('=');

            if (parts.Length == 2)
            {
                arguments[parts[0]] = parts[1];
            }
            else
            {
                arguments[arg] = null;
            }
        }

        return arguments;
    }

    public void PrintHelp()
    {
        Console.WriteLine("Help:");
        Console.WriteLine("------");
        Console.WriteLine("Usage: netscrapy config.json [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help             Display this help message");
        Console.WriteLine("  --verbose          Enable verbose mode");
        Console.WriteLine("  --output=<file>    Specify output file");
    }
}