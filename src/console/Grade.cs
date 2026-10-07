// FlashGrader: minimal sketch of grading one flashcard answer with an LLM.
// Uses raw HttpClient + System.Text.Json (no SDK) so the wire protocol is visible.
//
// Run:  set ANTHROPIC_API_KEY=...   (PowerShell: $env:ANTHROPIC_API_KEY="...")
//       dotnet run

using System.Text.Json.Serialization;

record Grade(
    [property: JsonPropertyName("verdict")] string Verdict,
    [property: JsonPropertyName("missing")] List<string> Missing,
    [property: JsonPropertyName("errors")] List<string> Errors,
    [property: JsonPropertyName("feedback")] string Feedback);
