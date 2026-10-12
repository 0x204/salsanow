using Newtonsoft.Json;

namespace SalsaNOW.Core.Json;

public sealed class TrueStringConverter : JsonConverter<bool>
{
    public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Boolean:
                return (bool)reader.Value!;
            case JsonToken.String:
                return string.Equals((string?)reader.Value, "true", StringComparison.OrdinalIgnoreCase);
            default:
                reader.Skip();
                return false;
        }
    }

    public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
    {
        writer.WriteValue(value ? "true" : "false");
    }
}
