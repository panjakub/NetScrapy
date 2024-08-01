class CommandLineParser
{
    public Dictionary<string, string> arg { get; set; }

    public CommandLineParser(string[] args)
    {
        if (args.Count() == 0 || args.Contains("--help"))
        {
            PrintHelp();
            return; // Exit the application
        }
        else
        {
            arg = ParseArguments(args);
        }
    }

    private Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>();

        foreach (var arg in args)
        {
            string[] parts = arg.Split('=');

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
        Console.WriteLine("Usage: CommandLineApp [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help             Display this help message");
        Console.WriteLine("  --verbose          Enable verbose mode");
        Console.WriteLine("  --output=<file>    Specify output file");
    }
}