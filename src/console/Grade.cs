namespace console;


using System.Text.Json.Serialization;

record Grade(
    [property: JsonPropertyName("verdict"), JsonRequired] Verdict Verdict,
    [property: JsonPropertyName("missing")] List<string> Missing,
    [property: JsonPropertyName("errors")] List<string> Errors,
    [property: JsonPropertyName("feedback")] string Feedback);
