using BlockDrop.Shared.Storage;
using BlockDrop.Shared.Contracts;

namespace BlockDrop.Tests.Shared;

public static class StorageChecks
{
    public static int Run()
    {
        var checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks++;
            Console.WriteLine("PASS " + name);
        }
        var saveDirectory = Path.Combine(Path.GetTempPath(), "BlockDropChecks-" + Guid.NewGuid().ToString("N"));
        try
        {
            var scores = new ScoreStore(saveDirectory);
            foreach (var score in new[] { 100, 500, 300, 200, 600, 400, 0 }) scores.AddRound(score);
            var reloaded = new ScoreStore(saveDirectory);
            Check(reloaded.Best == 600 && reloaded.Records.SequenceEqual(new[] { 600, 500, 400, 300, 200 }), "Five best records persist across launches");
            scores.RememberBest(800);
            scores.Flush();
            Check(new ScoreStore(saveDirectory).Best == 800, "Current best persists when pausing");
            File.WriteAllText(Path.Combine(saveDirectory, "scores.json"), "invalid json");
            Check(new ScoreStore(saveDirectory).Records.Length == 0, "Damaged save does not prevent startup");
            var blockedPath = Path.Combine(saveDirectory, "not-a-directory");
            File.WriteAllText(blockedPath, "file");
            var blocked = new ScoreStore(blockedPath);
            blocked.AddRound(50);
            Check(blocked.Best == 50 && blocked.Records[0] == 50, "Unavailable storage keeps game and records usable");

            var legacyText = "{\"Best\":900,\"Records\":[700,500]}";
            File.WriteAllText(Path.Combine(saveDirectory, "scores.json"), legacyText);
            var repository = new GameScoreRepository(saveDirectory);
            var tetris = new GameDefinition("tetris", "TETRIS", "LCD", "GIRAR", "SOLTAR", [], "scores.json");
            var snake = new GameDefinition("snake", "GUSANITO", "LCD", "ACCION", "ACCION", []);
            var migrated = repository.Open(tetris);
            Check(migrated.Best == 900 && migrated.Records.SequenceEqual([700, 500]) && File.Exists(Path.Combine(saveDirectory, "games", "tetris", "scores.json")), "Existing Tetris records migrate into its own folder");
            Check(File.ReadAllText(Path.Combine(saveDirectory, "scores.json")) == legacyText, "Migration preserves the original save file");
            var separate = repository.Open(snake);
            separate.AddRound(40);
            Check(repository.Open(snake).Best == 40 && repository.Open(tetris).Best == 900, "Game save folders do not share high scores");
            migrated.AddRound(1000);
            Check(repository.Open(tetris).Best == 1000, "Existing per-game save takes precedence over legacy data");
        }
        finally
        {
            var fullPath = Path.GetFullPath(saveDirectory);
            var testRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            if (Path.GetDirectoryName(fullPath) != testRoot || !Path.GetFileName(fullPath).StartsWith("BlockDropChecks-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected test cleanup target.");
            if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
        }
        
        return checks;
    }
}
