// FlashGrader: minimal sketch of grading one flashcard answer with an LLM.
// Uses raw HttpClient + System.Text.Json (no SDK) so the wire protocol is visible.
//
// Run:  set ANTHROPIC_API_KEY=...   (PowerShell: $env:ANTHROPIC_API_KEY="...")
//       dotnet run

using System.Text.Json.Serialization;
// Only the parts of the Messages API response this sketch needs.
record MessagesResponse(
    [property: JsonPropertyName("stop_reason")] string StopReason,
    [property: JsonPropertyName("content")] List<ContentBlock> Content);
