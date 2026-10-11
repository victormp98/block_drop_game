using BlockDrop.Games.Platformer;
using BlockDrop.Shared.Contracts;

namespace BlockDrop.Tests.Platformer;

public static class PlatformerChecks
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
        static void Advance(PlatformerGame game, double seconds)
        {
            for (var tick = 0; tick < (int)Math.Round(seconds * 120); tick++) game.Tick(1.0 / 120);
        }
        var game = new PlatformerGame([TestLevel()]);
        game.Tick(0.25);
        Check(!game.Started && game.Score == 0, "Platformer waits for START");
        game.Start();
        Check(game.Lives == 3 && game.LevelIndex == 0 && game.Grounded, "Platformer starts with three lives");
        var x = game.X;
        game.Move(1); Advance(game, 0.5);
        Check(game.X > x + 2 && Math.Abs(game.Y - 9.1) < 0.0001, "Platformer walks smoothly and stays on the floor");
        game.Release(1); x = game.X;
        Advance(game, 0.3);
        Check(game.X == x, "Releasing direction stops walking immediately");
        game.Jump(); Advance(game, 0.4);
        Check(game.Y < 6.3 && !game.Grounded && game.VelocityY < 0, "Platformer jump reaches elevated platforms");
        game.Jump(); Advance(game, 0.2);
        Check(game.VelocityY > 0, "Platformer cannot jump again in midair");
        Advance(game, 0.6);
        Check(game.Grounded && Math.Abs(game.Y - 9.1) < 0.0001, "Platformer lands without falling through tiles");
        game.ToggleRun(); game.Move(1); x = game.X;
        Advance(game, 0.3);
        Check(game.Running && game.X > x + 1.8, "Platformer running increases movement speed");
        game.SetPaused(true); x = game.X;
        var y = game.Y;
        game.Jump(); game.Move(-1); game.ToggleRun(); Advance(game, 1);
        Check(game.X == x && game.Y == y && game.Running, "Paused Platformer freezes physics and ignores controls");
        game.SetPaused(false); Advance(game, 0.3);
        Check(game.X == x, "Resuming does not leave a direction stuck down");
        game.Move(-1); Advance(game, 3);
        Check(game.X == 0, "Platformer respects the left world boundary");

        game = new PlatformerGame([TestLevel(wall: true)]);
        game.Start(); game.Move(1); Advance(game, 2);
        Check(Math.Abs(game.X - (5 - PlatformerGame.PlayerWidth)) < 0.001, "Solid walls stop horizontal movement");
        game = new PlatformerGame([TestLevel(ceiling: true)]);
        game.Start(); game.Jump(); Advance(game, 0.5);
        Check(game.Y >= 7, "Solid ceilings stop upward movement");

        game = new PlatformerGame([TestLevel(coin: true)]);
        game.Start(); game.Move(1); Advance(game, 1);
        Check(game.Score == 100 && game.Coins.Single().Collected, "Collecting a coin awards 100 points");
        game.Move(-1); Advance(game, 1);
        game.Move(1); Advance(game, 1);
        Check(game.Score == 100, "Collected coins cannot score twice");

        game = new PlatformerGame([TestLevel(enemy: true)]);
        game.Start();
        game.Enemies.Single().Direction = 0;
        game.Move(1); Advance(game, 0.6);
        game.Jump();
        while (game.X < 7.15) game.Tick(1.0 / 120);
        game.Release(1); Advance(game, 0.6);
        Check(!game.Enemies.Single().Alive && game.Score == 200 && game.Lives == 3, "Landing on an enemy defeats it and awards points");
        game = new PlatformerGame([TestLevel(enemy: true)]);
        game.Start(); game.Enemies.Single().Direction = 0;
        game.Move(1); Advance(game, 2);
        Check(game.Lives == 2 && game.Invincible > 0 && game.X == game.Checkpoint.X, "Side contact with an enemy loses a life and respawns safely");

        game = new PlatformerGame([TestLevel(gap: true, checkpoint: true)]);
        game.Start(); game.Move(1);
        for (var tick = 0; tick < 1000 && game.Lives == 3; tick++) game.Tick(1.0 / 120);
        Check(game.Lives == 2 && game.Checkpoint.X > 9 && game.X == game.Checkpoint.X, "Falling returns to the latest checkpoint");
        for (var life = 0; life < 2; life++)
        {
            game.Move(1);
            var before = game.Lives;
            for (var tick = 0; tick < 1000 && game.Lives == before; tick++) game.Tick(1.0 / 120);
        }
        Check(game.GameOver && !game.Won && game.Lives == 0, "Platformer ends after three failed attempts");
        x = game.X; y = game.Y;
        game.Move(-1); game.Jump(); Advance(game, 1);
        Check(game.X == x && game.Y == y, "Finished Platformer ignores time and controls");
        game.Start();
        Check(game.Score == 0 && game.Lives == 3 && !game.GameOver, "Platformer restart clears the previous adventure");

        var shortLevel = TestLevel(width: 10);
        game = new PlatformerGame([shortLevel, shortLevel]);
        game.Start(); game.Move(1); Advance(game, 1.8);
        Check(game.LevelIndex == 1 && game.IntroTime > 0 && game.Score == 1000 && !game.GameOver, "Reaching the flag advances to the next level");
        Advance(game, 1); game.Move(1); Advance(game, 1.8);
        Check(game.Won && game.GameOver && game.Score == 2600, "The last flag wins the adventure and awards the lives bonus");

        var module = new PlatformerModule([TestLevel()]);
        module.Start(); module.HandleInput(GameButton.Right); module.Update(0.1);
        module.ReleaseInput(GameButton.Right); x = module.Engine.X; module.Update(0.1);
        Check(module.Engine.X == x, "Platformer module handles button release");
        module.HandleInput(GameButton.A); module.Update(0.1);
        Check(module.Engine.VelocityY < 0, "Platformer module maps A to jump");
        module.HandleInput(GameButton.B);
        Check(module.Engine.Running, "Platformer module maps B to running");

        // Complete the shipped maps using normal walking, jumping and running inputs.
        // The driver reads terrain ahead, jumps at gaps/enemies, and never edits gameplay state.
        game = new PlatformerGame();
        game.Start(); game.ToggleRun();
        var visited = new HashSet<int>();
        for (var tick = 0; tick < 30000 && !game.GameOver; tick++)
        {
            visited.Add(game.LevelIndex);
            game.Move(1);
            var ahead = (int)Math.Floor(game.X + PlatformerGame.PlayerWidth + 0.6);
            var gapAhead = !game.Level.SolidAt(ahead, 10);
            var enemyAhead = game.Enemies.Any(enemy => enemy.Alive && enemy.X - game.X > -0.1 && enemy.X - game.X < 2.0);
            // Land safely after leaving a ledge before attempting the next gap.
            if (gapAhead && !game.Grounded && game.VelocityY > 0 && game.Level.SolidAt((int)(game.X + 0.3), 10)) game.Release(1);
            if (game.Grounded && (gapAhead || enemyAhead)) game.Jump();
            game.Tick(1.0 / 120);
        }
        Check(game.Won && visited.Count == 3 && game.Lives > 0, $"All three shipped platformer levels are completable with normal controls (level {game.LevelIndex + 1}, x={game.X:F2}, y={game.Y:F2})");
        var invalid = false;
        try { _ = new PlatformerLevel("S..F", "###"); } catch (ArgumentException) { invalid = true; }
        Check(invalid, "Malformed platformer maps are rejected");
        return count;
    }

    private static PlatformerLevel TestLevel(int width = 30, bool wall = false, bool ceiling = false,
        bool coin = false, bool enemy = false, bool gap = false, bool checkpoint = false)
    {
        var map = Enumerable.Range(0, 12).Select(_ => Enumerable.Repeat('.', width).ToArray()).ToArray();
        for (var column = 0; column < width; column++) map[10][column] = map[11][column] = '#';
        map[9][2] = 'S';
        map[9][width - 2] = 'F';
        if (wall) map[8][5] = map[9][5] = '#';
        if (ceiling) { map[6][2] = map[6][3] = '#'; }
        if (coin) map[9][6] = 'C';
        if (enemy) map[9][7] = 'E';
        if (checkpoint) map[9][9] = 'K';
        if (gap) for (var column = 14; column < 18; column++) map[10][column] = map[11][column] = '.';
        return new PlatformerLevel(map.Select(row => new string(row)).ToArray());
    }
}
