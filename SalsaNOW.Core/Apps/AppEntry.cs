using Newtonsoft.Json;
using SalsaNOW.Core.Json;

namespace SalsaNOW.Core.Apps;

public sealed class AppEntry
{
    public string Name { get; set; } = "";

    public string FileExtension { get; set; } = "";

    public string ExeName { get; set; } = "";

    [JsonConverter(typeof(TrueStringConverter))]
    public bool Run { get; set; }

    public string Url { get; set; } = "";

    public string? Version { get; set; }

    [JsonIgnore]
    public bool IsExecutable => string.Equals(FileExtension, "exe", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsArchive => string.Equals(FileExtension, "zip", StringComparison.OrdinalIgnoreCase);
}
