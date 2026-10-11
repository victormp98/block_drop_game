using Microsoft.Maui.Graphics;

namespace BlockDrop.Shared.Graphics;

public static class LcdGraphics
{
    public static readonly Color Paper = Color.FromArgb("#C8C8BE");
    public static readonly Color Ink = Color.FromArgb("#30312D");
    public static readonly Color Faint = Color.FromArgb("#B4B5AB");
    public static void Block(ICanvas c, float x, float y, float size, bool ghost)
    {
        var gap = Math.Max(0.8f, size * 0.08f);
        c.StrokeColor = ghost ? Faint : Ink;
        c.StrokeSize = ghost ? 1 : Math.Max(1, size * 0.075f);
        c.DrawRectangle(x + gap, y + gap, size - gap * 2, size - gap * 2);
        if (!ghost)
        {
            c.FillColor = Ink;
            c.FillRectangle(x + gap * 3, y + gap * 3, Math.Max(1, size - gap * 6), Math.Max(1, size - gap * 6));
        }
    }

    public static void Center(ICanvas c, string text, float width, float y, float size) => Text(c, text, (width - (text.Length * 6 - 1) * size) / 2, y, size);

    // A small bitmap alphabet keeps the LCD typography crisp without a font dependency.
    public static void Text(ICanvas c, string text, float x, float y, float size, Color? color = null)
    {
        c.FillColor = color ?? Ink;
        foreach (var character in text.ToUpperInvariant())
        {
            if (Glyphs.TryGetValue(character, out var glyph))
                for (var row = 0; row < 7; row++)
                for (var column = 0; column < 5; column++)
                    if ((glyph[row] & (1 << (4 - column))) != 0)
                        c.FillRectangle(x + column * size, y + row * size, size * 0.92f, size * 0.92f);
            x += 6 * size;
        }
    }

    private static readonly Dictionary<char, int[]> Glyphs = new()
    {
        ['A'] = [14,17,17,31,17,17,17], ['B'] = [30,17,17,30,17,17,30],
        ['C'] = [14,17,16,16,16,17,14], ['D'] = [30,17,17,17,17,17,30],
        ['E'] = [31,16,16,30,16,16,31], ['F'] = [31,16,16,30,16,16,16],
        ['G'] = [14,17,16,23,17,17,15], ['H'] = [17,17,17,31,17,17,17],
        ['I'] = [31,4,4,4,4,4,31], ['J'] = [7,2,2,2,18,18,12],
        ['K'] = [17,18,20,24,20,18,17], ['L'] = [16,16,16,16,16,16,31],
        ['M'] = [17,27,21,21,17,17,17], ['N'] = [17,25,21,19,17,17,17],
        ['O'] = [14,17,17,17,17,17,14], ['P'] = [30,17,17,30,16,16,16],
        ['Q'] = [14,17,17,17,21,18,13], ['R'] = [30,17,17,30,20,18,17],
        ['S'] = [15,16,16,14,1,1,30], ['T'] = [31,4,4,4,4,4,4],
        ['U'] = [17,17,17,17,17,17,14], ['V'] = [17,17,17,17,17,10,4],
        ['W'] = [17,17,17,21,21,21,10], ['X'] = [17,17,10,4,10,17,17],
        ['Y'] = [17,17,10,4,4,4,4], ['Z'] = [31,1,2,4,8,16,31],
        ['0'] = [14,17,19,21,25,17,14], ['1'] = [4,12,4,4,4,4,14],
        ['2'] = [14,17,1,2,4,8,31], ['3'] = [30,1,1,14,1,1,30],
        ['4'] = [2,6,10,18,31,2,2], ['5'] = [31,16,16,30,1,1,30],
        ['6'] = [14,16,16,30,17,17,14], ['7'] = [31,1,2,4,8,8,8],
        ['8'] = [14,17,17,14,17,17,14], ['9'] = [14,17,17,15,1,1,14],
        ['>'] = [0,8,4,2,4,8,0], [':'] = [0,4,4,0,4,4,0],
        ['.'] = [0,0,0,0,0,12,12], ['/'] = [1,2,2,4,8,8,16],
        ['-'] = [0,0,0,31,0,0,0]
    };
}
