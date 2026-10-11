using BlockDrop.Shared.Contracts;
using BlockDrop.Shared.Graphics;
using Microsoft.Maui.Graphics;
using static BlockDrop.Shared.Graphics.LcdGraphics;

namespace BlockDrop.Games.Racing;

public sealed class RacingScreen(RacingGame game)
{
    private static readonly string[] Car = ["..####..", ".######.", "##....##", "##.##.##", ".######.", ".######.", ".######.", "##.##.##", "##....##", ".######.", "..####..", "..#..#.."];
    public void Draw(ICanvas c, RectF bounds, GameDrawContext context)
    {
        var w = bounds.Width;
        var h = bounds.Height;
        Text(c, "CAR " + game.Lives, 14, 12, 1.3f);
        Text(c, $"PTS {game.Score:D6}", w - 115, 13, 1.1f);
        var roadWidth = Math.Min(w - 66, (h - 68) * 0.65f);
        var road = new RectF((w - roadWidth) / 2, 38, roadWidth, h - 68);
        c.FillColor = Paper;
        c.FillRectangle(road);
        c.StrokeColor = Ink;
        c.StrokeSize = 2;
        c.DrawRectangle(road);
        var laneWidth = roadWidth / 3;
        c.FillColor = Faint;
        for (var lane = 1; lane <= 2; lane++)
        for (double position = game.Distance % 16 - 16; position < 110; position += 16)
            PixelArt.Fill(c, road.X + lane * laneWidth - 1, road.Y + (float)(position / 110) * road.Height, 2, road.Height * 0.065f, road);
        var pixel = Math.Min(laneWidth / 11, road.Height / 110);
        void DrawCar(int lane, double position, bool player)
        {
            var x = road.X + (lane + 0.5f) * laneWidth - pixel * 4;
            var y = road.Y + (float)(position / 110) * road.Height - pixel * 6;
            PixelArt.Draw(c, Car, x, y, pixel, road, mirror: !player);
            if (player)
            {
                c.FillColor = Paper;
                PixelArt.Fill(c, x + 3 * pixel, y + 4 * pixel, 2 * pixel, 3 * pixel, road);
            }
        }
        foreach (var car in game.Traffic) DrawCar(car.Lane, car.Position, false);
        if (game.Invincible <= 0 || (int)(game.Invincible * 10) % 2 == 0) DrawCar(game.Lane, RacingGame.PlayerPosition, true);
        Text(c, "T", road.X - 15, road.Y + road.Height - 40, 1f);
        c.StrokeColor = Ink;
        c.StrokeSize = 1;
        c.DrawRectangle(road.X - 15, road.Y + road.Height - 26, 8, 25);
        c.FillColor = Ink;
        var fuelHeight = (float)game.Fuel * 0.21f;
        c.FillRectangle(road.X - 13, road.Y + road.Height - 3 - fuelHeight, 4, fuelHeight);
        Center(c, game.Turbo ? "TURBO" : game.Braking ? "FRENANDO" : "A: TURBO  B: FRENO", w, h - 15, 1f);
    }
}
