using System.Text.Json;

namespace BlockDrop.Shared.Storage;

public sealed class ScoreStore
{
    private readonly string path;
    private bool dirty;
    public int Best { get; private set; }
    public int[] Records { get; private set; } = [];

    public ScoreStore(string directory, string? legacyPath = null)
    {
        path = Path.Combine(directory, "scores.json");
        try
        {
            var source = File.Exists(path) ? path : legacyPath;
            if (source is null || !File.Exists(source)) return;
            var saved = JsonSerializer.Deserialize<ScoreData>(File.ReadAllText(source));
            if (saved is null) return;
            Records = (saved.Records ?? []).Where(score => score > 0).OrderDescending().Take(5).ToArray();
            Best = Math.Max(Math.Max(0, saved.Best), Records.FirstOrDefault());
            if (source != path) { dirty = true; Flush(); }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing, damaged or unavailable save must not prevent playing.
        }
    }

    public void RememberBest(int score)
    {
        if (score <= Best) return;
        Best = score;
        dirty = true;
    }

    public void AddRound(int score)
    {
        if (score <= 0) return;
        RememberBest(score);
        Records = Records.Append(score).OrderDescending().Take(5).ToArray();
        dirty = true;
        Flush();
    }

    public void Flush()
    {
        if (!dirty) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new ScoreData(Best, Records)));
            File.Move(temporary, path, overwrite: true);
            dirty = false;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Keep the record in memory and retry at the next save point.
        }
    }

    private sealed record ScoreData(int Best, int[]? Records);
}
