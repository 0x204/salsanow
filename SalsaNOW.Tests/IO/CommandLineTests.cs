using System.Text;
using SalsaNOW.Core.IO;

namespace SalsaNOW.Tests.IO;

public class CommandLineTests
{
    [Theory]
    [InlineData("simple", "\"simple\"")]
    [InlineData("https://example.com/?a=1&b=2", "\"https://example.com/?a=1&b=2\"")]
    [InlineData(@"https://example.com/path\", "\"https://example.com/path\\\\\"")]
    [InlineData("", "\"\"")]
    [InlineData("with space", "\"with space\"")]
    [InlineData(@"C:\Program Files\app\", "\"C:\\Program Files\\app\\\\\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    public void Quote(string argument, string expected)
    {
        Assert.Equal(expected, CommandLine.Quote(argument));
    }

    [Theory]
    [InlineData("https://example.com/?a=1&b=2")]
    [InlineData(@"C:\path with spaces\")]
    [InlineData(@"trailing\\")]
    [InlineData("quote\"inside")]
    [InlineData("backslash before quote \\\"x")]
    [InlineData("\t tabs and  spaces ")]
    [InlineData("")]
    public void Quote_RoundTripsThroughTheWindowsParser(string argument)
    {
        Assert.Equal(argument, Assert.Single(ParseWindowsArguments(CommandLine.Quote(argument))));
    }

    private static List<string> ParseWindowsArguments(string commandLine)
    {
        var arguments = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        bool hasArgument = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '\\')
            {
                int backslashes = 0;
                while (i < commandLine.Length && commandLine[i] == '\\')
                {
                    backslashes++;
                    i++;
                }

                if (i < commandLine.Length && commandLine[i] == '"')
                {
                    current.Append('\\', backslashes / 2);
                    if (backslashes % 2 == 1)
                        current.Append('"');
                    else
                        inQuotes = !inQuotes;
                }
                else
                {
                    current.Append('\\', backslashes);
                    i--;
                }

                hasArgument = true;
            }
            else if (c == '"')
            {
                inQuotes = !inQuotes;
                hasArgument = true;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (hasArgument)
                    arguments.Add(current.ToString());
                current.Clear();
                hasArgument = false;
            }
            else
            {
                current.Append(c);
                hasArgument = true;
            }
        }

        if (hasArgument)
            arguments.Add(current.ToString());

        return arguments;
    }
}
