using System.Text.Json.Serialization;

/// <summary>The question file (civics-test-2025.json): metadata plus the list of questions.</summary>
internal record Library(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("notes")] List<string> Notes,
    [property: JsonPropertyName("categories")] List<string> Categories,
    [property: JsonPropertyName("questions"), JsonRequired] List<LibraryQuestion> Questions);

/// <summary>
/// One question with all valid "answers"; "count" (default 1) is how many distinct answers the student must give.
/// </summary>
internal record LibraryQuestion(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("question"), JsonRequired] string Question,
    [property: JsonPropertyName("answers"), JsonRequired] List<string> Answers,
    [property: JsonPropertyName("count")] int Count = 1,
    // For applicants 65+ with 20+ years as a permanent resident.
    [property: JsonPropertyName("sixtyFiveTwenty")] bool SixtyFiveTwenty = false,
    // Answers given are for Washington State.
    [property: JsonPropertyName("stateSpecific")] bool StateSpecific = false,
    // Answer depends on the official serving at the time of the interview.
    [property: JsonPropertyName("changesWithOfficeholder")] bool ChangesWithOfficeholder = false);
