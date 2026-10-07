// FlashGrader: minimal sketch of grading one flashcard answer with an LLM.
// Uses raw HttpClient + System.Text.Json (no SDK) so the wire protocol is visible.
//
// Run:  set ANTHROPIC_API_KEY=...   (PowerShell: $env:ANTHROPIC_API_KEY="...")
//       dotnet run

using System.Text.Json.Serialization;

internal record Library([property: JsonPropertyName("cards")] List<LibraryCard> Cards);

/// <summary>
/// A card has either a single "answer", or "accepted" (valid answers) plus "count" (how many the student must give).
/// </summary>
internal record LibraryCard(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("answer")] string? Answer = null,
    [property: JsonPropertyName("accepted")] List<string>? Accepted = null,
    [property: JsonPropertyName("count")] int? Count = null);
