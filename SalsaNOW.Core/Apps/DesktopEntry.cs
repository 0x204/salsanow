using Newtonsoft.Json;
using SalsaNOW.Core.Json;

namespace SalsaNOW.Core.Apps;

public sealed class DesktopEntry
{
    public string Name { get; set; } = "";

    public string ExeName { get; set; } = "";

    [JsonConverter(typeof(TrueStringConverter))]
    public bool Run { get; set; }

    public string Url { get; set; } = "";

    [JsonIgnore]
    public string ReleaseFileName => Path.GetFileName(new Uri(Url).AbsolutePath);
}
