namespace console;


using System.Text.Json.Serialization;

record ContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text);
