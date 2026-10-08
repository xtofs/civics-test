// FlashGrader: minimal sketch of grading one flashcard answer with an LLM.
// Uses LlmClient to encapsulate HttpClient while keeping the wire protocol visible.
//
// Run: start LM Studio's server on port 1234 and load a chat LLM (not an embedding model).
//      dotnet run --project src/FlashGrader

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;


internal class Program
{

    private static async Task Main(string[] args)
    {
        if (Debugger.IsAttached) { Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", ".."); }

        var log = new Log("logs");

        Console.CancelKeyPress += (s, e) =>
        {
            // Cleanup code here
            Console.WriteLine("Termination requested. Cleaning up...");
            Console.WriteLine();
            ShowStatistics(log);
        };

        using var llm = new LlmClient(new Uri("http://127.0.0.1:1234/"));

        var model = await llm.GetLoadedModelAsync();
        if (model is null)
        {
            Console.Error.WriteLine("No chat LLM loaded in LM Studio. Load a chat model in the Developer tab; embedding models cannot grade answers.");
            Environment.ExitCode = 1;
            return;
        }
        Console.WriteLine($"Using model: {model}");


        var library = LoadCardLibrary();
        var weights = CardWeights(library.Cards, log.LoadLogEntries());
        var picked = Random.Shared.SampleProportional(weights, Math.Min(SessionSize, weights.Length));
        var cards = picked.Select(i => Card.From(library.Cards[i])).ToList();

        Console.WriteLine($"testing {SessionSize} random cards");
        for (int i = 0; i < SessionSize; i++)
        {
            var card = cards[i];
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            await AskAsync(card, llm, model, log);
        }

        ShowStatistics(log);
    }

    private const int SessionSize = 20;

    // Credit for a "partial" answer: between none (0) and half (0.5).
    private const float PartialCredit = 0.25f;


    // Weight per card in (0, 1): the smoothed share of credit missed. "correct" earns 1, "partial" PartialCredit, "incorrect" 0.
    // Always-correct cards approach 0, always-wrong cards approach 1, unseen cards start at 0.5.
    // The +1 / +2 smoothing keeps every weight strictly above 0, so no card is ever excluded from sampling.
    private static float[] CardWeights(IReadOnlyList<LibraryCard> cards, IEnumerable<LogEntry> history)
    {
        var stats = history
            .Where(e => e.Verdict is not Verdict.Ungraded) // ungraded says nothing
            .GroupBy(e => e.Question)
            .ToDictionary(g => g.Key, g => (
                Total: g.Count(),
                Credit: g.Sum(e => e.Verdict switch
                {
                    Verdict.Correct => 1f,
                    Verdict.Partial => PartialCredit,
                    Verdict.Incorrect or Verdict.Ungraded => 0f,
                    _ => throw new UnreachableException($"Unknown verdict {e.Verdict}"),
                })));

        var weights = cards.Select(c =>
        {
            var (total, credit) = stats.GetValueOrDefault(c.Question);
            return (total - credit + 1f) / (total + 2f);
        }).ToArray();

        // SampleProportional requires every weight <= sum / k; cap (rarely needed) until that holds.
        var k = Math.Min(SessionSize, weights.Length);
        for (var pass = 0; pass < 20 && k > 0; pass++)
        {
            var cap = weights.Sum() / k;
            if (weights.Max() <= cap * (1 + 1e-6f)) break;
            for (var i = 0; i < weights.Length; i++) weights[i] = Math.Min(weights[i], cap);
        }
        return weights;
    }

    private static Library LoadCardLibrary()
    {
        var cards = JsonSerializer.Deserialize<Library>(File.ReadAllText("cards.json"))!;
        foreach (var c in cards.Cards) Card.From(c); // fail fast on malformed cards
        var rand = Random.Shared; // new Random(0);
        rand.Shuffle(CollectionsMarshal.AsSpan(cards.Cards));
        return cards;
    }

    private static async Task<bool> AskAsync(Card card, LlmClient llm, string model, Log log)
    {
        Console.WriteLine(card.Question);
        Console.Write("> ");
        var answer = Console.ReadLine() ?? "";


        var grade = await llm.GradeAsync(card, answer, model);
        log.AppendEntry(new LogEntry(DateTimeOffset.Now, card.Question, answer, grade?.Verdict ?? Verdict.Ungraded));

        if (grade is null)
        {
            Console.WriteLine("Could not grade automatically - fall back to self-grading.");
            return false;
        }

        var color = grade.Verdict switch
        {
            Verdict.Correct => ConsoleColor.Green,
            Verdict.Partial => ConsoleColor.Yellow,
            Verdict.Incorrect => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
        Console.Write($"\nVerdict: ");
        Console.ForegroundColor = color;
        Console.WriteLine($"{grade.Verdict}");
        Console.ResetColor();

        if (grade.Missing.Count > 0) Console.WriteLine($"Missing:  {string.Join("; ", grade.Missing)}");
        if (grade.Errors.Count > 0) Console.WriteLine($"Errors:   {string.Join("; ", grade.Errors)}");
        if (!string.IsNullOrWhiteSpace(grade.Feedback)) { Console.WriteLine($"Feedback: {grade.Feedback}"); }
        switch (card)
        {
            case SingleAnswerCard single:
                Console.WriteLine($"Expected: {single.Answer}");
                break;
            case MultiAnswerCard multi:
                var shown = multi.Accepted.Take(10);
                var more = multi.Accepted.Count > 10 ? $"; +{multi.Accepted.Count - 10} more" : "";
                Console.WriteLine($"Accepted ({multi.RequiredCount} needed): {string.Join("; ", shown)}{more}");
                break;
        }

        return true;
    }

    private static void ShowStatistics(Log log)
    {
        var history = log.LoadLogEntries();
        var counts = history.GroupBy(entry => entry.Verdict)
                            .ToDictionary(g => g.Key, g => g.Count());
        var sum = counts.Values.Sum();
        var histogram = counts
            .OrderBy(e => e.Key) // enum order: Correct, Partial, Incorrect, Ungraded
            .Select(e => new { Key = e.Key.ToString(), e.Value, Percentage = (double)e.Value / sum * 100 })
            .Append(new { Key = "Total", Value = sum, Percentage = 100.0 });

        foreach (var group in histogram)
        {
            Console.WriteLine($"    {group.Key,-10}: {group.Value,-4} ({group.Percentage:F2}%)");
        }
    }
}


public static class SamplingExtensions
{
    extension(Random rng)
    {
        public int[] SampleProportional(ReadOnlySpan<float> weights, int k)
        {
            int n = weights.Length;
            ArgumentOutOfRangeException.ThrowIfNegative(k);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(k, n);

            double total = 0; // accumulate in double to limit rounding error
            foreach (float w in weights)
            {
                if (!(w >= 0) || float.IsInfinity(w))
                    throw new ArgumentOutOfRangeException(nameof(weights));
                total += w;
            }
            if (k == 0) return [];

            double step = total / k;
            foreach (float w in weights)
                if (w > step * (1 + 1e-5)) // tolerate float rounding from the caller's capping
                    throw new ArgumentException("Weight too large for exact proportional inclusion.");

            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            rng.Shuffle(order);                     // randomizes which items co-occur

            var result = new int[k];
            double u = rng.NextDouble() * step;     // first point
            double cum = 0;
            int j = 0;
            for (int t = 0; t < n && j < k; t++)
            {
                int i = order[t];
                // treat the last segment as open-ended to absorb rounding error
                cum = t == n - 1 ? double.PositiveInfinity : cum + weights[i];
                if (u < cum)                         // at most one point per segment
                {
                    result[j++] = i;
                    u += step;
                }
            }
            return result;
        }
    }
}