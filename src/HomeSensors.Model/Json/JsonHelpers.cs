using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeSensors.Model.Json;

public static class JsonHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = GetOptions();

    private static JsonSerializerOptions GetOptions()
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
        };

        serializerOptions.Converters.Add(new ConverterDateTimeOffset());
        serializerOptions.Converters.Add(new ConverterString());

        return serializerOptions;
    }
}
