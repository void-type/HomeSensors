using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeSensors.Model.Infrastructure.HomeAssistant;

public class EntityHistoryState
{
    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement> Attributes { get; set; } = [];

    [JsonPropertyName("last_changed")]
    public DateTimeOffset? LastChanged { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTimeOffset? LastUpdated { get; set; }
}
