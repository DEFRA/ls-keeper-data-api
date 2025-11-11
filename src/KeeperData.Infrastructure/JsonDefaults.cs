using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Infrastructure;

public static class JsonDefaults
{
    private static JsonSerializerOptions s_defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { }
    };

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

    private static JsonSerializerOptions s_defaultOptionsDataBridgeApiSupport = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new SafeNullableIntConverter(),
            new SafeNullableShortConverter(),
            new SafeNullableDecimalConverter(),
            new SafeNullableBoolConverter(),
            new SafeNullableCharConverter(),
            new SafeDateTimeConverter(),
            new SafeNullableDateTimeConverter()
        }
    };

    public static JsonSerializerOptions DefaultOptions
    {
        get => s_defaultOptions;
        set => s_defaultOptions = value;
    }

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

    public static JsonSerializerOptions DefaultOptionsWithDataBridgeApiSupport
    {
        get => s_defaultOptionsDataBridgeApiSupport;
        set => s_defaultOptionsDataBridgeApiSupport = value;
    }
}