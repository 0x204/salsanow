using Newtonsoft.Json;

namespace SalsaNOW.Core.Json;

public static class JsonConfig
{
    public static T Parse<T>(string json) where T : class
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json)
                ?? throw new InvalidDataException("The document is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(ex.Message, ex);
        }
    }
}
