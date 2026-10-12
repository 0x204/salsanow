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
}
