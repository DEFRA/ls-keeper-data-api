using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Infrastructure;

public static class JsonDefaults
{
    private static JsonSerializerOptions s_defaultOptionsWithStringEnumConversion = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private static JsonSerializerOptions s_defaultOptionsWithIndented = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static JsonSerializerOptions s_defaultOptionsWithSnsPascalSupport = new()
    {
        PropertyNamingPolicy = null, // Pascal
        WriteIndented = false
    };

    public static JsonSerializerOptions DefaultOptionsWithStringEnumConversion
    {
        get => s_defaultOptionsWithStringEnumConversion;
        set => s_defaultOptionsWithStringEnumConversion = value;
    }

    public static JsonSerializerOptions DefaultOptionsWithIndented
    {
        get => s_defaultOptionsWithIndented;
        set => s_defaultOptionsWithIndented = value;
    }

    public static JsonSerializerOptions DefaultOptionsWithSnsPascalSupport
    {
        get => s_defaultOptionsWithSnsPascalSupport;
        set => s_defaultOptionsWithSnsPascalSupport = value;
    }
}