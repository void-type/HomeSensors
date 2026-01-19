using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeSensors.Model.Infrastructure.Json.Converters;

public class ConverterDateTimeOffset : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // A less strict DateTimeOffset deserialization.
#pragma warning disable S6580
        return DateTimeOffset.Parse(reader.GetString() ?? string.Empty);
#pragma warning restore S6580
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
