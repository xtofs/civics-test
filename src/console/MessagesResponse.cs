namespace console;

using System.Text.Json.Serialization;
// Only the parts of the Messages API response this sketch needs.
record MessagesResponse(
    [property: JsonPropertyName("stop_reason")] string StopReason,
    [property: JsonPropertyName("content")] List<ContentBlock> Content);
