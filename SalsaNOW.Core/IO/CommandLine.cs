using System.Text;

namespace SalsaNOW.Core.IO;

public static class CommandLine
{
    public static string Quote(string argument)
    {
        var quoted = new StringBuilder(argument.Length + 2);
        quoted.Append('"');

        int backslashes = 0;
        foreach (char c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            if (c == '"')
                quoted.Append('\\', backslashes * 2 + 1);
            else
                quoted.Append('\\', backslashes);

            quoted.Append(c);
            backslashes = 0;
        }

        quoted.Append('\\', backslashes * 2);
        quoted.Append('"');
        return quoted.ToString();
    }
}
