using Microsoft.Maui.Graphics;
using BlockDrop.Shared.Contracts;
using static BlockDrop.Shared.Graphics.LcdGraphics;

namespace BlockDrop.Games.Tetris;

internal sealed class TetrisScreen(TetrisGame game)
{
    public void Draw(ICanvas c, RectF bounds, GameDrawContext context)
    {
        var w = bounds.Width;
        var h = bounds.Height;
        Text(c, "BLOCKDROP", 14, 12, 1.5f);
        var cell = Math.Min((h - 68) / TetrisGame.Rows, (w - 112) / TetrisGame.Columns);
        var boardWidth = TetrisGame.Columns * cell;
        var x = (w - boardWidth - 92) / 2;
        var y = 40f + Math.Max(0, (h - 68 - TetrisGame.Rows * cell) / 2);
        c.StrokeColor = Ink;
        c.StrokeSize = 2;
        c.DrawRectangle(x - 3, y - 3, boardWidth + 6, TetrisGame.Rows * cell + 6);
        for (var row = 0; row < TetrisGame.Rows; row++)
        for (var column = 0; column < TetrisGame.Columns; column++)
        {
            if (game.Board[row, column] != 0) Block(c, x + column * cell, y + row * cell, cell, false);
            else
            {
                c.StrokeColor = Faint;
                c.StrokeSize = 0.5f;
                c.DrawRectangle(x + column * cell + 2, y + row * cell + 2, Math.Max(1, cell - 4), Math.Max(1, cell - 4));
            }
        }
        if (!game.GameOver)
        {
            foreach (var part in TetrisGame.PieceCells(game.Piece, game.Rotation))
            {
                var ghostRow = game.GhostY + part.Y;
                if (ghostRow >= 0) Block(c, x + (game.X + part.X) * cell, y + ghostRow * cell, cell, true);
            }
            foreach (var part in TetrisGame.PieceCells(game.Piece, game.Rotation))
                if (game.Y + part.Y >= 0) Block(c, x + (game.X + part.X) * cell, y + (game.Y + part.Y) * cell, cell, false);
        }

        var sideX = x + boardWidth + 15;
        var step = Math.Min(57, (TetrisGame.Rows * cell - 50) / 4);
        Stat(c, "PUNTOS", game.Score.ToString("D6"), sideX, y + 3);
        Stat(c, "RECORD", context.Best.ToString("D6"), sideX, y + 3 + step);
        Stat(c, "LINEAS", game.Lines.ToString("D3"), sideX, y + 3 + 2 * step);
        Stat(c, "NIVEL", game.Level.ToString("D2"), sideX, y + 3 + 3 * step);
        Text(c, "SIGUIENTE", sideX, y + 4 * step, 1f);
        var nextCells = TetrisGame.PieceCells(game.NextPiece).ToArray();
        var minX = nextCells.Min(part => part.X);
        var minY = nextCells.Min(part => part.Y);
        foreach (var part in nextCells) Block(c, sideX + (part.X - minX) * 12, y + 4 * step + 19 + (part.Y - minY) * 12, 12, false);
        Center(c, "A GIRAR  B SOLTAR  START PAUSA", w, h - 15, 0.95f);
    }

    private static void Stat(ICanvas c, string title, string value, float x, float y)
    {
        Text(c, title, x, y, 1f);
        Text(c, value, x, y + 15, 1.65f);
    }

}
