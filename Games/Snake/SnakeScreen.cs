using BlockDrop.Shared.Contracts;
using Microsoft.Maui.Graphics;
using static BlockDrop.Shared.Graphics.LcdGraphics;

namespace BlockDrop.Games.Snake;

public sealed class SnakeScreen(SnakeGame game)
{
    public void Draw(ICanvas c, RectF bounds, GameDrawContext context)
    {
        var w = bounds.Width;
        var h = bounds.Height;
        Text(c, "SNAKE", 14, 12, 1.5f);
        Text(c, $"PTS {game.Score:D5}", w - 105, 13, 1.1f);
        var size = Math.Min((w - 32) / SnakeGame.Columns, (h - 70) / SnakeGame.Rows);
        var x = (w - size * SnakeGame.Columns) / 2;
        var y = 38 + (h - 70 - size * SnakeGame.Rows) / 2;
        c.FillColor = Paper;
        c.FillRectangle(x, y, size * SnakeGame.Columns, size * SnakeGame.Rows);
        c.StrokeColor = Ink;
        c.StrokeSize = 2;
        c.DrawRectangle(x - 3, y - 3, size * SnakeGame.Columns + 6, size * SnakeGame.Rows + 6);
        foreach (var cell in game.Body) Block(c, x + cell.X * size, y + cell.Y * size, size, false);
        if (game.Body.Count > 0)
        {
            var head = game.Body[0];
            c.FillColor = Ink;
            c.FillRectangle(x + head.X * size + 2, y + head.Y * size + 2, size - 4, size - 4);
            c.FillColor = Paper;
            var eye = Math.Max(1, size * 0.15f);
            c.FillRectangle(x + head.X * size + size * 0.6f, y + head.Y * size + size * 0.25f, eye, eye);
        }
        if (game.Started && !game.Won)
        {
            var fx = x + game.Food.X * size;
            var fy = y + game.Food.Y * size;
            c.FillColor = Ink;
            c.FillRectangle(fx + size * 0.3f, fy + size * 0.2f, size * 0.4f, size * 0.6f);
            c.FillRectangle(fx + size * 0.2f, fy + size * 0.3f, size * 0.6f, size * 0.4f);
        }
        Center(c, "A: DER  B: IZQ  START: PAUSA", w, h - 15, 1f);
    }
}
