/// <summary>Command line options. <see cref="Category"/> null means questions from any category.</summary>
internal sealed record CommandLineOptions(int Count, string? Category)
{
    public const int DefaultCount = 20;

    public const string Usage = """
        Usage: FlashGrader [options]

        Options:
          -n, --count <N>          Number of questions to ask (default 20).
          -c, --category <name>    Only ask questions from this category (default: any category).
          -h, --help               Show this help.
        """;

    /// <summary>Parses <paramref name="args"/>; returns null and sets <paramref name="error"/> on invalid input or help.</summary>
    public static CommandLineOptions? Parse(string[] args, out string? error)
    {
        var count = DefaultCount;
        string? category = null;
        error = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help" or "-?":
                    return null;

                case "-n" or "--count":
                    if (++i >= args.Length || !int.TryParse(args[i], out count) || count < 1)
                    {
                        error = $"{args[i - 1]} expects a positive number.";
                        return null;
                    }
                    break;

                case "-c" or "--category":
                    if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]))
                    {
                        error = $"{args[i - 1]} expects a category name.";
                        return null;
                    }
                    category = args[i];
                    break;

                default:
                    error = $"Unknown argument \"{args[i]}\".";
                    return null;
            }
        }
        return new CommandLineOptions(count, category);
    }
}
