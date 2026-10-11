using BlockDrop.Games.Racing;
using BlockDrop.Shared.Contracts;

namespace BlockDrop.Tests.Racing;

public static class RacingChecks
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
        static void Advance(RacingGame game, double seconds)
        {
            for (var tick = 0; tick < (int)Math.Round(seconds * 120); tick++) game.Tick(1.0 / 120);
        }
        var game = new RacingGame(new FixedRandom());
        game.Tick(0.25);
        Check(!game.Started && game.Score == 0, "Racing waits for START");
        game.Start();
        Check(game.Lane == 1 && game.Lives == 3 && game.Fuel == 100 && game.Traffic.Count == 1, "Racing starts with three lives and a full tank");
        game.Move(-1); game.Move(-1);
        Check(game.Lane == 0, "Racing clamps the left road edge");
        game.Move(1); game.Move(1); game.Move(1);
        Check(game.Lane == 2, "Racing clamps the right road edge");
        var normalSpeed = game.Speed;
        game.Accelerate();
        Check(game.Turbo && game.Speed > normalSpeed, "Racing turbo increases road speed");
        Advance(game, 0.5);
        Check(game.Fuel < 80 && game.Distance > normalSpeed * 0.5, "Turbo consumes fuel and covers extra distance");
        var fuel = game.Fuel;
        game.Brake();
        Check(game.Braking && !game.Turbo && game.Speed < normalSpeed, "Braking cancels turbo and reduces speed");
        Advance(game, 0.5);
        Check(game.Fuel > fuel, "Fuel regenerates while turbo is off");
        Advance(game, 1);
        Check(!game.Braking && !game.Turbo, "Temporary speed effects expire");
        var distance = game.Distance;
        fuel = game.Fuel;
        game.SetPaused(true);
        game.Move(-1); game.Accelerate(); game.Brake(); Advance(game, 2);
        Check(game.Lane == 2 && game.Distance == distance && game.Fuel == fuel, "Paused Racing freezes traffic, fuel and controls");
        game.SetPaused(false);
        Advance(game, 3);
        Check(game.Score >= (int)(game.Distance * 2) + 50 && game.Lives == 3, "Passing traffic awards points without a collision");
        Check(game.Traffic.All(car => car.Position <= 112), "Cars are removed after passing the road");

        game.Start();
        game.Move(-1);
        for (var tick = 0; tick < 2000 && game.Lives == 3; tick++) game.Tick(1.0 / 120);
        Check(game.Lives == 2 && game.Invincible > 0 && !game.GameOver, "A collision costs one life and grants a recovery period");
        var lives = game.Lives;
        Advance(game, 0.5);
        Check(game.Lives == lives, "Recovery prevents overlapping collisions from taking multiple lives");
        for (var tick = 0; tick < 5000 && !game.GameOver; tick++) game.Tick(1.0 / 120);
        Check(game.GameOver && game.Lives == 0, "Racing ends when all three lives are lost");
        distance = game.Distance;
        var lane = game.Lane;
        game.Move(1); game.Accelerate(); Advance(game, 1);
        Check(game.Distance == distance && game.Lane == lane, "Finished Racing ignores frames and controls");
        game.Start();
        Check(game.Score == 0 && game.Lives == 3 && game.Level == 1 && !game.GameOver, "Racing restart resets distance and difficulty");
        var module = new RacingModule(new FixedRandom());
        module.Start();
        module.HandleInput(GameButton.A);
        Check(module.Engine.Turbo, "Racing module maps A to turbo");
        module.HandleInput(GameButton.Down);
        Check(module.Engine.Braking, "Racing module maps DOWN to brake");
        module.Update(double.NaN); module.Update(-1);
        Check(module.Engine.Distance == 0, "Racing rejects invalid frame durations");
        return count;
    }

    private sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }
}
