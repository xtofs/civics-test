// FlashGrader: minimal sketch of grading one flashcard answer with an LLM.
// Uses raw HttpClient + System.Text.Json (no SDK) so the wire protocol is visible.
//
// Run:  set ANTHROPIC_API_KEY=...   (PowerShell: $env:ANTHROPIC_API_KEY="...")
//       dotnet run

using System.Text.Json;

abstract record Card(string Question)
{
    /// <summary>All answers the grader should treat as valid.</summary>
    public abstract IReadOnlyList<string> Accepted { get; }

    /// <summary>How many distinct valid items the student must give.</summary>
    public abstract int RequiredCount { get; }

    /// <summary>Validates a library entry: it needs either "answer", or "accepted" plus "count".</summary>
    internal static Card From(LibraryCard x)
    {
        if (x.Answer is not null && x.Accepted is null && x.Count is null)
            return new SingleAnswerCard(x.Question, x.Answer);

        if (x.Answer is null && x.Accepted is { Count: > 0 } && x.Count is >= 1 && x.Count <= x.Accepted.Count)
            return new MultiAnswerCard(x.Question, x.Accepted, x.Count.Value);

        throw new JsonException(
            $"Card \"{x.Question}\": use either \"answer\", or \"accepted\" (non-empty) with \"count\" (1..accepted.Length).");
    }
}

sealed record SingleAnswerCard(string Question, string Answer) : Card(Question)
{
    public override IReadOnlyList<string> Accepted => [Answer];
    public override int RequiredCount => 1;
}

sealed record MultiAnswerCard(string Question, IReadOnlyList<string> Accepted, int Required) : Card(Question)
{
    public override IReadOnlyList<string> Accepted { get; } = Accepted;
    public override int RequiredCount => Required;
}
