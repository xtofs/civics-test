namespace console;


// Run: start LM Studio's server on port 1234 and load a chat LLM (not an embedding model).
//      dotnet run --project src/console -- [--count N] [--category name]

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;


internal class Program
{

    private static async Task Main(string[] args)
    {
        if (Debugger.IsAttached) { Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", ".."); }

        var options = CommandLineOptions.Parse(args, out var error);
        if (options is null)
        {
            if (error is not null) Console.Error.WriteLine(error);
            (error is null ? Console.Out : Console.Error).WriteLine(CommandLineOptions.Usage);
            Environment.ExitCode = error is null ? 0 : 2;
            return;
        }

        var library = LoadCardLibrary();
        var questions = library.Questions;
        if (options.Category is not null)
        {
            // Only categories that actually have questions are valid choices.
            var used = questions.GroupBy(q => q.Category).ToDictionary(g => g.Key, g => g.Count());
            var category = used.Keys.FirstOrDefault(c => c.Equals(options.Category, StringComparison.OrdinalIgnoreCase));
            if (category is null)
            {
                Console.Error.WriteLine($"Unknown category \"{options.Category}\". Available categories:");
                foreach (var c in library.Categories.Where(used.ContainsKey))
                    Console.Error.WriteLine($"    {c,-18} {used[c],3} questions");
                Environment.ExitCode = 2;
                return;
            }
            questions = questions.Where(q => q.Category == category).ToList();
        }

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


        var sessionSize = Math.Min(options.Count, questions.Count);
        var weights = CardWeights(questions, log.LoadLogEntries(), sessionSize);
        var picked = Random.Shared.SampleProportional(weights, sessionSize);
        var cards = picked.Select(i => Card.From(questions[i])).ToList();

        var from = options.Category is null ? "" : $" from category {questions[0].Category}";
        Console.WriteLine($"testing {cards.Count} random cards{from}");
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            await AskAsync(i, card, llm, model, log);
        }

        ShowStatistics(log);
    }

    // Credit for a "partial" answer: between none (0) and half (0.5).
    private const float PartialCredit = 0.25f;


    // Weight per card in (0, 1): the smoothed share of credit missed. "correct" earns 1, "partial" PartialCredit, "incorrect" 0.
    // Always-correct cards approach 0, always-wrong cards approach 1, unseen cards start at 0.5.
    // The +1 / +2 smoothing keeps every weight strictly above 0, so no card is ever excluded from sampling.
    private static float[] CardWeights(IReadOnlyList<LibraryQuestion> cards, IEnumerable<LogEntry> history, int sessionSize)
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
        var k = Math.Min(sessionSize, weights.Length);
        for (var pass = 0; pass < 20 && k > 0; pass++)
        {
            var cap = weights.Sum() / k;
            if (weights.Max() <= cap * (1 + 1e-6f)) break;
            for (var i = 0; i < weights.Length; i++) weights[i] = Math.Min(weights[i], cap);
        }
        return weights;
    }

    private const string LibraryFile = "civics-test-2025.json";

    private static Library LoadCardLibrary()
    {
        var library = JsonSerializer.Deserialize<Library>(File.ReadAllText(LibraryFile))
            ?? throw new JsonException($"{LibraryFile} is empty.");
        foreach (var q in library.Questions)
        {
            Card.From(q); // fail fast on malformed questions
            if (!library.Categories.Contains(q.Category))
                throw new JsonException($"Question {q.Number}: unknown category \"{q.Category}\".");
        }
        var rand = Random.Shared; // new Random(0);
        rand.Shuffle(CollectionsMarshal.AsSpan(library.Questions));
        return library;
    }

    private static async Task<bool> AskAsync(int index, Card card, LlmClient llm, string model, Log log)
    {
        Console.WriteLine($"Question {index + 1}: {card.Question}");
        Console.Write("> ");
        var answer = Console.ReadLine() ?? "";


        var grade = await llm.GradeAsync(card, answer, model);
        log.AppendEntry(new LogEntry(DateTimeOffset.Now, card.Question, answer, grade?.Verdict ?? Verdict.Ungraded));

        if (grade is null)
        {
            Console.WriteLine("Could not grade automatically - fall back to self-grading.");
            return false;
        }

        Console.Clear();
        Console.WriteLine($"Answer: {answer}");

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
        var counts = history.GroupBy(entry => entry.Verdict == Verdict.Correct ? "Correct" : "Incorrect")
                            .ToDictionary(g => g.Key, g => g.Count());
        var sum = counts.Values.Sum();
        var histogram = counts
            .OrderBy(e => e.Key)
            .Select(e => new
            {
                Key = e.Key,
                Count = e.Value,
                Percentage = $"{((double)e.Value / sum * 100):F2}%",
            })
            .Append(new { Key = "Total", Count = sum, Percentage = "" });


        Console.WriteLine(new string('=', 80));
        Console.WriteLine("   ");
        foreach (var group in histogram)
        {
            Console.WriteLine($"    {group.Key,-10}: {group.Count,4} {group.Percentage}");
        }
    }
}
