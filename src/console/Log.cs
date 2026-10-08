using System.Text.Json;

internal sealed class Log
{
    private readonly string _directory;

    public Log(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    public void AppendEntry(LogEntry entry)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            var line = JsonSerializer.Serialize(entry);
            File.AppendAllText(Path.Combine(_directory, $"{entry.Time:yyyy-MM-dd}.jsonl"), line + Environment.NewLine);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Could not write log: {ex.Message}");
        }
    }

    public List<LogEntry> LoadLogEntries()
    {
        var entries = new List<LogEntry>();
        if (!Directory.Exists(_directory)) return entries;

        foreach (var file in Directory.EnumerateFiles(_directory, "*.jsonl"))
        {
            foreach (var line in File.ReadLines(file))
            {
                try
                {
                    if (JsonSerializer.Deserialize<LogEntry>(line) is { } entry) entries.Add(entry);
                }
                catch (JsonException) { }
            }
        }

        return entries;
    }
}
