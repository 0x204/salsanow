namespace SalsaNOW.Configuration;

internal sealed class CommandLineOptions
{
    public string? CustomAppsJson { get; private set; }

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--apps-json" or "-a")
                options.CustomAppsJson = args[++i];
        }

        return options;
    }
}
