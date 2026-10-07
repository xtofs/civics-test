using System.Text.Json.Serialization;

record ChatCompletionResponse(
    [property: JsonPropertyName("choices")] List<ChatCompletionChoice>? Choices);

record ChatCompletionChoice(
    [property: JsonPropertyName("finish_reason")] string? FinishReason,
    [property: JsonPropertyName("message")] ChatCompletionMessage? Message);

record ChatCompletionMessage(
    [property: JsonPropertyName("content")] string? Content);
