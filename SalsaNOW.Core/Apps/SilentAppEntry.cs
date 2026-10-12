using Newtonsoft.Json;
using SalsaNOW.Core.Json;

namespace SalsaNOW.Core.Apps;

public sealed class SilentAppEntry
{
    public string Name { get; set; } = "";

    public string FileName { get; set; } = "";

    public string FileExtension { get; set; } = "";

    [JsonConverter(typeof(TrueStringConverter))]
    public bool Archive { get; set; }

    [JsonConverter(typeof(TrueStringConverter))]
    public bool Run { get; set; }

    public string Url { get; set; } = "";

    public string? Version { get; set; }

    [JsonIgnore]
    public string ExecutableName => $"{FileName}.{FileExtension}";

    public bool IsNamed(string name)
    {
        name = name.Trim();
        return string.Equals(Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(FileName, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ExecutableName, name, StringComparison.OrdinalIgnoreCase);
    }
}
