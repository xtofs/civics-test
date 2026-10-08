using System.Text.Json;
using System.Text.Json.Serialization;

// Serialized in camelCase ("correct", "partial", ...) to match the LLM schema and existing log files.
[JsonConverter(typeof(VerdictJsonConverter))]
enum Verdict
{
    Correct,
    Partial,
    Incorrect,

    /// <summary>The answer could not be graded automatically; only used in the log, never returned by the LLM.</summary>
    Ungraded,
}

sealed class VerdictJsonConverter() : JsonStringEnumConverter<Verdict>(JsonNamingPolicy.CamelCase, allowIntegerValues: false);
