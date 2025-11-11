using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Converters;

public class SafeNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return DateTime.TryParse(str, out var value) ? value : null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToString("O"));
    }
}