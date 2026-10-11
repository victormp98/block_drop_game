using BlockDrop.Games.Snake;
using BlockDrop.Shared.Contracts;

namespace BlockDrop.Tests.Snake;

public static class SnakeChecks
{
    public static int Run()
    {
        var count = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            count++;
            System.Console.WriteLine("PASS " + message);
        }
        static void Step(SnakeGame engine) => engine.Tick(engine.StepSeconds + 0.00001);
        var game = new SnakeGame(new FoodRandom());
        game.Tick(0.25);
        Check(!game.Started && game.Body.Count == 0, "Snake waits for START");
        game.Start();
        Check(game.Body.Count == 4 && game.Score == 0 && !game.GameOver, "Snake starts with four segments");
        Check(game.Food == new SnakeCell(9, 9) && !game.Body.Contains(game.Food), "Food occupies an empty cell");
        game.Turn(SnakeDirection.Left);
        Step(game);
        Check(game.Body[0] == new SnakeCell(9, 9) && game.Direction == SnakeDirection.Right, "Snake cannot reverse into its neck");
        Check(game.Score == 100 && game.Body.Count == 5 && game.StepSeconds < 0.22, "Food grows the snake, scores points and increases speed");
        Check(!game.Body.Contains(game.Food), "Replacement food avoids the enlarged body");
        game.Turn(SnakeDirection.Up);
        game.Turn(SnakeDirection.Left);
        Step(game);
        Check(game.Direction == SnakeDirection.Up && game.Body[0] == new SnakeCell(9, 8), "Only one turn is queued per movement step");
        game.Turn(SnakeDirection.Left);
        Step(game);
        game.Turn(SnakeDirection.Down);
        Step(game);
        Check(game.GameOver && !game.Won, "Snake detects collision with its own body");
        var head = game.Body[0];
        game.Turn(SnakeDirection.Right);
        game.Tick(0.25);
        Check(game.Body[0] == head && game.Score == 100, "Finished Snake ignores time and controls");
        game.Start();
        Check(!game.GameOver && !game.Paused && game.Score == 0 && game.Body.Count == 4, "Snake restart clears the previous round");

        var tail = new SnakeGame(new Random(1));
        tail.Start();
        tail.Turn(SnakeDirection.Up); Step(tail);
        tail.Turn(SnakeDirection.Left); Step(tail);
        tail.Turn(SnakeDirection.Down); Step(tail);
        Check(!tail.GameOver && tail.Body[0] == new SnakeCell(7, 9), "Snake can safely enter a cell its tail vacates");
        head = tail.Body[0];
        tail.SetPaused(true);
        tail.Turn(SnakeDirection.Left);
        tail.Tick(0.25);
        Check(tail.Body[0] == head && tail.Direction == SnakeDirection.Down, "Paused Snake ignores gameplay input");
        tail.SetPaused(false);
        Step(tail);
        Check(tail.Body[0] != head, "Snake resumes its existing body");
        var wall = new SnakeGame(new Random(3));
        wall.Start();
        for (var index = 0; index < 25; index++) Step(wall);
        Check(wall.GameOver && wall.Body.All(cell => cell.X >= 0 && cell.X < SnakeGame.Columns), "Snake stops at the arena wall");
        var module = new SnakeModule(new Random(4));
        module.Start();
        module.HandleInput(GameButton.A); Step(module.Engine);
        Check(module.Engine.Direction == SnakeDirection.Down, "Snake A turns clockwise");
        module.HandleInput(GameButton.B); Step(module.Engine);
        Check(module.Engine.Direction == SnakeDirection.Right, "Snake B turns counterclockwise");
        var before = module.Engine.Body[0];
        module.Update(double.NaN);
        module.Update(-1);
        Check(module.Engine.Body[0] == before, "Snake rejects invalid frame durations");
        return count;
    }

    private sealed class FoodRandom : Random
    {
        private bool first = true;
        public override int Next(int maxValue)
        {
            if (!first) return 0;
            first = false;
            return 185; // Empty-cell index of (9, 9) in the opening arena.
        }
    }
}
