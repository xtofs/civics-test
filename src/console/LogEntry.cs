using System.Text.Json.Serialization;


// {"time":"2026-10-06T14:01:14.100361-07:00","question":"If the President can no longer serve, who becomes President?","answer":"The Vice President","verdict":"correct"}
internal record LogEntry(
    [property: JsonPropertyName("time")]
    DateTimeOffset Time,
    [property: JsonPropertyName("question")]
    string Question,
    [property: JsonPropertyName("answer")]
    string Answer,
    [property: JsonPropertyName("verdict"), JsonRequired]
    Verdict Verdict
);