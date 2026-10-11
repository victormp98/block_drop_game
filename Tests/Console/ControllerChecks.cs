using BlockDrop.ConsoleUI;
using BlockDrop.Games.Tetris;
using BlockDrop.Shared.Contracts;
using BlockDrop.Shared.Storage;
using Microsoft.Maui.Graphics;

namespace BlockDrop.Tests.ConsoleUI;

public static class ControllerChecks
{
    public static int Run()
    {
        var checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks++;
            System.Console.WriteLine("PASS " + name);
        }
        void Reject(Action action, string name)
        {
            var rejected = false;
            try { action(); } catch (ArgumentException) { rejected = true; }
            Check(rejected, name);
        }

        var testRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        var directory = Path.Combine(testRoot, "BlockDropConsoleChecks-" + Guid.NewGuid().ToString("N"));
        try
        {
            var repository = new GameScoreRepository(directory);
            var tetris = new TetrisModule(new Random(42));
            var console = new ConsoleController(new GameCatalog([tetris]), repository.Open);
            Check(console.Mode == ScreenMode.Menu && !tetris.Started && console.MenuEntries[0].Label == "JUGAR", "Single-game startup preserves the existing menu");
            console.HandleButton(GameButton.Start);
            Check(console.Mode == ScreenMode.Playing && tetris.Started && !tetris.Paused, "START launches the registered Tetris module");
            var x = tetris.Engine.X;
            console.HandleButton(GameButton.Left);
            Check(tetris.Engine.X == x - 1, "Console routes direction input to Tetris");
            var rotation = tetris.Engine.Rotation;
            console.HandleButton(GameButton.A);
            Check(tetris.Engine.Rotation == (tetris.Engine.Piece == 1 ? rotation : (rotation + 1) & 3), "Module owns the meaning of button A");
            console.HandleButton(GameButton.B);
            Check(tetris.Engine.Board.Cast<int>().Count(cell => cell > 0) == 4 && console.View.Best == tetris.Score, "Module drop updates the board and shared high score");
            var score = tetris.Score;
            console.HandleButton(GameButton.Start);
            var y = tetris.Engine.Y;
            console.Update(0.25);
            console.HandleButton(GameButton.Down);
            Check(console.Mode == ScreenMode.Paused && tetris.Engine.Y == y && tetris.Score == score, "Console pause stops frames and gameplay input");
            console.HandleButton(GameButton.Start);
            Check(console.Mode == ScreenMode.Playing && !tetris.Paused, "START resumes without restarting the game");
            console.HandleButton(GameButton.Select);
            Check(console.Mode == ScreenMode.Menu && tetris.Paused && console.MenuEntries[0].Action == MenuAction.Resume, "SELECT retains a resumable session");
            var recordIndex = console.MenuEntries.ToList().FindIndex(entry => entry.Action == MenuAction.Records);
            console.SelectMenuItem(recordIndex);
            Check(console.Mode == ScreenMode.Records && console.View.Game == tetris && console.View.Best == score, "Records refer to the current game");
            console.HandleButton(GameButton.B);
            console.SelectMenuItem(console.MenuEntries.ToList().FindIndex(entry => entry.Action == MenuAction.Help));
            Check(console.Mode == ScreenMode.Help && console.View.Game.Definition.HelpLines.Contains("ARRIBA / A: GIRAR"), "Game supplies its own help and button descriptions");
            console.HandleButton(GameButton.Select);
            console.SelectMenuItem(console.MenuEntries.ToList().FindIndex(entry => entry.Action == MenuAction.NewGame));
            Check(tetris.Score == 0 && tetris.Engine.Board.Cast<int>().All(cell => cell == 0) && console.View.Records.SequenceEqual([score]), "New round saves the previous score and resets only the game");
            console.Close();

            // Test modules exist only in this test project, never in the application menu.
            var first = new TestGame("check-one", "PRIMERO", 10);
            var second = new TestGame("check-two", "SEGUNDO", 20);
            var multi = new ConsoleController(new GameCatalog([first, second]), repository.Open);
            Check(multi.MenuEntries.Take(2).Select(entry => entry.Label).SequenceEqual(["PRIMERO", "SEGUNDO"]), "Menu discovers every registered game");
            multi.SelectMenuItem(1);
            Check(multi.CurrentGame == second && second.Starts == 1 && first.Starts == 0, "Selecting another game activates its module");
            multi.HandleButton(GameButton.Up);
            Check(second.LastButton == GameButton.Up && first.LastButton is null, "Input goes exclusively to the active game");
            multi.Update(0.1);
            Check(second.Updates == 1 && first.Updates == 0, "Only the active game receives frame updates");
            multi.HandleButton(GameButton.A);
            multi.HandleButton(GameButton.Select);
            Check(second.Paused && multi.View.Best == 20, "Returning to the menu pauses and remembers that game's score");
            multi.SelectMenuItem(multi.MenuEntries.ToList().FindIndex(entry => entry.GameId == first.Definition.Id));
            Check(multi.CurrentGame == first && first.Score == 0 && multi.View.Best == 0 && second.Paused, "Switching games isolates state and scores");
            multi.HandleButton(GameButton.A);
            multi.PlayGame(second.Definition.Id);
            Check(multi.CurrentGame == second && second.Starts == 1 && second.Score == 20 && first.Paused && multi.View.Best == 20, "Switching back resumes the original session");
            Reject(() => multi.PlayGame("missing"), "Unknown games are rejected");
            Check(multi.CurrentGame == second && multi.Mode == ScreenMode.Playing && !second.Paused, "Invalid game choice leaves the active session intact");
            second.Finish();
            multi.Update(0.1);
            Check(multi.Mode == ScreenMode.GameOver && multi.View.Records.SequenceEqual([20]), "Shared game-over handling saves the correct game's score");
            multi.Update(0.1);
            multi.HandleButton(GameButton.Down);
            Check(second.Score == 20 && multi.View.Records.Count == 1, "Finished rounds cannot keep scoring or save twice");
            multi.Close();
            multi.Close();
            Check(repository.Open(first.Definition).Records.SequenceEqual([10]) && repository.Open(second.Definition).Records.SequenceEqual([20]), "Each game's records persist separately and close is idempotent");
            multi.HandleButton(GameButton.Start);
            Check(second.Starts == 2 && second.Score == 0 && multi.View.Best == 20, "Restart retains only that game's record");
            Check(multi.Back() && multi.Mode == ScreenMode.Paused && multi.Back() && multi.Mode == ScreenMode.Menu && !multi.Back(), "Back pauses, returns to menu, then allows exiting");
            Reject(() => new GameCatalog([]), "Empty game library is rejected");
            Reject(() => new GameCatalog([first, first]), "Duplicate game IDs are rejected");
            Reject(() => new GameCatalog([new TestGame("../invalid", "INVALIDO", 0)]), "Game ID cannot escape its save folder");
            var library = GameLibrary.Create();
            Check(library.Games.Select(game => game.Definition.Id).SequenceEqual(["tetris", "snake", "racing", "platformer"]), "Production library contains four implemented games");
            var actual = new ConsoleController(library, repository.Open);
            Check(actual.MenuEntries.Take(4).Select(entry => entry.GameId).SequenceEqual(["tetris", "snake", "racing", "platformer"]), "All four games appear in the startup menu");
            foreach (var registered in library.Games)
            {
                actual.PlayGame(registered.Definition.Id);
                Check(registered.Started && !registered.Paused && actual.Mode == ScreenMode.Playing, registered.Definition.Id + " starts through the real catalog");
                actual.HandleButton(GameButton.Right);
                actual.Update(0.1);
                actual.ReleaseButton(GameButton.Right);
                actual.HandleButton(GameButton.Start);
                Check(registered.Paused && actual.Mode == ScreenMode.Paused, registered.Definition.Id + " supports shared pause");
            }
            actual.PlayGame("platformer");
            var runner = (BlockDrop.Games.Platformer.PlatformerModule)actual.CurrentGame;
            actual.HandleButton(GameButton.Right);
            actual.Update(0.1);
            actual.ReleaseButton(GameButton.Right);
            var runnerX = runner.Engine.X;
            actual.Update(0.1);
            Check(runner.Engine.X == runnerX, "Console delivers release events for smooth platformer controls");
            actual.Close();
            var goal = new BlockDrop.Games.Platformer.PlatformerLevel("..........", "..S.....F.", "##########");
            var winningGame = new BlockDrop.Games.Platformer.PlatformerModule([goal]);
            var winningConsole = new ConsoleController(new GameCatalog([winningGame]), repository.Open);
            winningConsole.HandleButton(GameButton.Start);
            winningConsole.HandleButton(GameButton.Right);
            for (var tick = 0; tick < 200 && !winningGame.GameOver; tick++) winningConsole.Update(1.0 / 60);
            Check(winningGame.Won && winningConsole.Mode == ScreenMode.GameOver && winningConsole.View.Records.Contains(winningGame.Score), "A real platformer victory uses the shared result screen and saves its record");
            var winningScore = winningGame.Score;
            winningConsole.HandleButton(GameButton.Start);
            Check(!winningGame.Won && winningGame.Score == 0 && winningConsole.View.Best == winningScore, "START restarts a won adventure while retaining its own best score");
            winningConsole.Close();
        }
        finally
        {
            var fullPath = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(fullPath) != testRoot || !Path.GetFileName(fullPath).StartsWith("BlockDropConsoleChecks-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected test cleanup target.");
            if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
        }
        return checks;
    }

    private sealed class TestGame(string id, string title, int points) : IRetroGame
    {
        public GameDefinition Definition { get; } = new(id, title, "TEST SYSTEM", "SALTAR", "ACCION", ["A: SALTAR"]);
        public bool Started { get; private set; }
        public bool Paused { get; private set; }
        public bool GameOver { get; private set; }
        public int Score { get; private set; }
        public int Starts { get; private set; }
        public int Updates { get; private set; }
        public GameButton? LastButton { get; private set; }
        public void Start() { Starts++; Started = true; Paused = GameOver = false; Score = 0; }
        public void SetPaused(bool paused) => Paused = paused;
        public void Update(double seconds) { if (!Paused && !GameOver) Updates++; }
        public void HandleInput(GameButton button) { LastButton = button; if (button == GameButton.A) Score += points; }
        public void Finish() => GameOver = true;
        public void Draw(ICanvas canvas, RectF bounds, GameDrawContext context) { }
    }
}
