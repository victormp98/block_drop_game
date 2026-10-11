using BlockDrop.Shared.Contracts;
using BlockDrop.Shared.Graphics;
using Microsoft.Maui.Graphics;
using static BlockDrop.Shared.Graphics.LcdGraphics;

namespace BlockDrop.Games.Platformer;

public sealed class PlatformerScreen(PlatformerGame game)
{
    private static readonly string[] Player = ["..####..", ".######.", ".#..#.#.", ".######.", "...##...", ".######.", "##.##.##", "...##...", "..#..#..", ".##..##."];
    private static readonly string[] Enemy = ["..####..", ".######.", "##.##.##", "########", "########", ".######.", "##....##", ".#....#."];
    private static readonly string[] Coin = ["..##..", ".#..#.", "#.#..#", "#.#..#", ".#..#.", "..##.."];
    private static readonly string[] Cloud = ["....####....", "..########..", "############", ".##########."];
    private static readonly string[] Flag = ["#.......", "#######.", "######..", "#####...", "#.......", "#.......", "#.......", "#.......", "#.......", "###....."];

    public void Draw(ICanvas c, RectF bounds, GameDrawContext context)
    {
        var w = bounds.Width;
        var h = bounds.Height;
        Text(c, $"RUN {game.LevelIndex + 1}/{game.LevelCount}  V {game.Lives}", 14, 12, 1.1f);
        Text(c, $"PTS {game.Score:D6}", w - 113, 12, 1.1f);
        var tile = Math.Min((h - 70) / game.Level.Height, (w - 28) / 16);
        var fieldWidth = Math.Min(w - 28, tile * 22);
        var field = new RectF((w - fieldWidth) / 2, 38, fieldWidth, h - 70);
        var camera = Math.Clamp(game.X - field.Width / tile * 0.35, 0, Math.Max(0, game.Level.Width - field.Width / tile));
        float SX(double worldX) => field.X + (float)(worldX - camera) * tile;
        float SY(double worldY) => field.Bottom - (float)(game.Level.Height - worldY) * tile;
        c.FillColor = Paper;
        c.FillRectangle(field);
        for (var cloud = 0; cloud < 5; cloud++)
            PixelArt.Draw(c, Cloud, SX(cloud * 13 + 3), SY(2 + cloud % 2), tile / 7, field, color: Faint);

        for (var y = 0; y < game.Level.Height; y++)
        for (var x = (int)camera; x < Math.Min(game.Level.Width, Math.Ceiling(camera + field.Width / tile)); x++)
        {
            if (!game.Level.SolidAt(x, y)) continue;
            c.FillColor = Faint;
            PixelArt.Fill(c, SX(x), SY(y), tile, tile, field);
            c.FillColor = Ink;
            PixelArt.Fill(c, SX(x), SY(y), tile, Math.Max(1, tile * 0.12f), field);
            PixelArt.Fill(c, SX(x) + tile * 0.1f, SY(y) + tile * 0.4f, tile * 0.3f, Math.Max(1, tile * 0.1f), field);
            PixelArt.Fill(c, SX(x) + tile * 0.6f, SY(y) + tile * 0.7f, tile * 0.3f, Math.Max(1, tile * 0.1f), field);
        }
        foreach (var point in game.Level.Checkpoints)
            PixelArt.Draw(c, Flag, SX(point.X), SY(point.Y - 0.2), tile / 10, field,
                color: point.X <= game.Checkpoint.X ? Ink : Faint);
        PixelArt.Draw(c, Flag, SX(game.Level.Goal.X), SY(game.Level.Goal.Y - 1), tile / 5, field);
        foreach (var coin in game.Coins.Where(coin => !coin.Collected))
            PixelArt.Draw(c, Coin, SX(coin.Position.X), SY(coin.Position.Y), tile / 12, field);
        foreach (var enemy in game.Enemies.Where(enemy => enemy.Alive))
            PixelArt.Draw(c, Enemy, SX(enemy.X), SY(enemy.Y), tile * 0.09f, field, mirror: enemy.Direction < 0);
        if (game.Invincible <= 0 || (int)(game.Invincible * 10) % 2 == 0)
            PixelArt.Draw(c, Player, SX(game.X), SY(game.Y), tile * 0.087f, field, mirror: game.FacingLeft);
        c.StrokeColor = Ink;
        c.StrokeSize = 1;
        c.DrawRectangle(field);
        if (game.IntroTime > 0)
        {
            c.FillColor = Paper;
            c.FillRectangle(w / 2 - 65, field.Y + field.Height / 2 - 15, 130, 30);
            Center(c, "NIVEL " + (game.LevelIndex + 1), w, field.Y + field.Height / 2 - 7, 2f);
        }
        Center(c, game.Running ? "A: SALTAR  B: CAMINAR" : "A: SALTAR  B: CORRER", w, h - 15, 1f);
    }
}
