using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Converters;

public class SafeNullableShortConverter : JsonConverter<short?>
{
    public override short? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return short.TryParse(str, out var value) ? value : null;
    }

    public override void Write(Utf8JsonWriter writer, short? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToString());
    }
}
