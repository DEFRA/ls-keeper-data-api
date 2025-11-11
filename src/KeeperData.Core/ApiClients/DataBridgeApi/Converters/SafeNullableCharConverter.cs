using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Converters;

public class SafeNullableCharConverter : JsonConverter<char?>
{
    public override char? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return !string.IsNullOrEmpty(str) && str.Length == 1 ? str[0] : null;
    }

    public override void Write(Utf8JsonWriter writer, char? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToString());
    }
}