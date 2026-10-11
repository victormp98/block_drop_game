using Microsoft.Maui.Graphics;

namespace BlockDrop.Shared.Graphics;

public static class PixelArt
{
    // Clip individual pixels so scrolling sprites never paint over the HUD or outside the LCD.
    public static void Draw(ICanvas canvas, IReadOnlyList<string> rows, float x, float y, float pixel,
        RectF clip, bool mirror = false, Color? color = null)
    {
        canvas.FillColor = color ?? LcdGraphics.Ink;
        for (var row = 0; row < rows.Count; row++)
        for (var column = 0; column < rows[row].Length; column++)
        {
            if (rows[row][column] != '#') continue;
            var drawColumn = mirror ? rows[row].Length - 1 - column : column;
            Fill(canvas, x + drawColumn * pixel, y + row * pixel, pixel, pixel, clip);
        }
    }

    public static void Fill(ICanvas canvas, float x, float y, float width, float height, RectF clip)
    {
        var left = Math.Max(x, clip.Left);
        var top = Math.Max(y, clip.Top);
        var right = Math.Min(x + width, clip.Right);
        var bottom = Math.Min(y + height, clip.Bottom);
        if (right > left && bottom > top) canvas.FillRectangle(left, top, right - left, bottom - top);
    }
}
