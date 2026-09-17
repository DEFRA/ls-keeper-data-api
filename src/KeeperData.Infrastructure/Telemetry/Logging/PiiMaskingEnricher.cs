using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace KeeperData.Infrastructure.Telemetry.Logging;

public class PiiMaskingEnricher : ILogEventEnricher
{
    // Elastic's ECS enricher stores a deferred HTTP-context object under this property. The ECS
    // formatter expands it later, including the raw query string, so it cannot be safely redacted
    // by walking Serilog property values.
    private const string EcsHttpContextPropertyName = "HttpContext";
    private static readonly Regex EmailRegex = new Regex(
        @"(?<=email=)[^&]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250));
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor? _httpContextAccessor;

    public PiiMaskingEnricher(Microsoft.AspNetCore.Http.IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var propertiesToUpdate = new List<LogEventProperty>();
        var propertiesToRemove = new List<string>();

        foreach (var property in logEvent.Properties)
        {
            if (string.Equals(property.Key, EcsHttpContextPropertyName, StringComparison.Ordinal)
                && RequestContainsEmailQueryParameter())
            {
                propertiesToRemove.Add(property.Key);
                continue;
            }

            var maskedValue = MaskValue(property.Value);
            if (!ReferenceEquals(maskedValue, property.Value))
            {
                propertiesToUpdate.Add(new LogEventProperty(property.Key, maskedValue));
            }
        }

        foreach (var property in propertiesToUpdate)
        {
            logEvent.AddOrUpdateProperty(property);
        }

        foreach (var propertyName in propertiesToRemove)
        {
            logEvent.RemovePropertyIfPresent(propertyName);
        }
    }

    private bool RequestContainsEmailQueryParameter() =>
        _httpContextAccessor?.HttpContext?.Request.Query.ContainsKey("email") == true;

    private static LogEventPropertyValue MaskValue(LogEventPropertyValue value) =>
        value switch
        {
            ScalarValue scalar => MaskScalar(scalar),
            StructureValue structure => MaskStructure(structure),
            DictionaryValue dictionary => MaskDictionary(dictionary),
            SequenceValue sequence => MaskSequence(sequence),
            _ => value
        };

    private static LogEventPropertyValue MaskScalar(ScalarValue scalar)
    {
        if (scalar.Value is string s && s.Contains("email=", StringComparison.OrdinalIgnoreCase))
        {
            return new ScalarValue(EmailRegex.Replace(s, "***"));
        }

        return scalar;
    }

    private static LogEventPropertyValue MaskStructure(StructureValue structure)
    {
        var properties = structure.Properties;
        var newProperties = new LogEventProperty[properties.Count];
        var mutated = false;

        for (var i = 0; i < properties.Count; i++)
        {
            var p = properties[i];
            var maskedPValue = MaskValue(p.Value);
            if (!ReferenceEquals(maskedPValue, p.Value))
            {
                mutated = true;
                newProperties[i] = new LogEventProperty(p.Name, maskedPValue);
            }
            else
            {
                newProperties[i] = p;
            }
        }

        return mutated ? new StructureValue(newProperties, structure.TypeTag) : structure;
    }

    private static LogEventPropertyValue MaskDictionary(DictionaryValue dictionary)
    {
        var elements = dictionary.Elements;
        var newElements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(elements.Count);
        var mutated = false;

        foreach (var kvp in elements)
        {
            var maskedKey = MaskValue(kvp.Key) as ScalarValue ?? kvp.Key;
            var maskedValue = MaskValue(kvp.Value);
            if (!ReferenceEquals(maskedKey, kvp.Key) || !ReferenceEquals(maskedValue, kvp.Value))
            {
                mutated = true;
            }
            newElements.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(maskedKey, maskedValue));
        }

        return mutated ? new DictionaryValue(newElements) : dictionary;
    }

    private static LogEventPropertyValue MaskSequence(SequenceValue sequence)
    {
        var seqElements = sequence.Elements;
        var newSeqElements = new LogEventPropertyValue[seqElements.Count];
        var mutated = false;

        for (var i = 0; i < seqElements.Count; i++)
        {
            var maskedElem = MaskValue(seqElements[i]);
            if (!ReferenceEquals(maskedElem, seqElements[i]))
            {
                mutated = true;
            }
            newSeqElements[i] = maskedElem;
        }

        return mutated ? new SequenceValue(newSeqElements) : sequence;
    }
}