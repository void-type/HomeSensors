using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeSensors.Model.Json;

public class ConverterString : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        // Adds the ability to deserialize from Number to string.
        JsonTokenType.Number => reader.GetInt64().ToString(),
        _ => reader.GetString(),
    };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        // For performance, lift up the writer implementation.
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.AsSpan());
        }
    }
}
