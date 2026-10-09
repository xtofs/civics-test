namespace console;



using System.Text.Json;

abstract record Card(string Question)
{
    /// <summary>All answers the grader should treat as valid.</summary>
    public abstract IReadOnlyList<string> Accepted { get; }

    /// <summary>How many distinct valid items the student must give.</summary>
    public abstract int RequiredCount { get; }

    /// <summary>Validates a library question: non-empty "answers" and a "count" of 1..answers.Length.</summary>
    internal static Card From(LibraryQuestion x)
    {
        if (x.Answers is not { Count: > 0 } || x.Answers.Any(string.IsNullOrWhiteSpace) || x.Count < 1 || x.Count > x.Answers.Count)
            throw new JsonException(
                $"Question {x.Number} \"{x.Question}\": needs non-empty \"answers\" and \"count\" in 1..answers.Length.");

        return x.Answers.Count == 1
            ? new SingleAnswerCard(x.Question, x.Answers[0])
            : new MultiAnswerCard(x.Question, x.Answers, x.Count);
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
