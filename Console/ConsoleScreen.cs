using BlockDrop.Shared.Contracts;
using Microsoft.Maui.Graphics;
using static BlockDrop.Shared.Graphics.LcdGraphics;

namespace BlockDrop.ConsoleUI;

public sealed class ConsoleScreen(Func<ConsoleViewState> readView) : IDrawable
{
    public bool Blink { get; set; }
    private float scale = 1;
    private float offsetX;
    private float offsetY;
    private readonly List<(RectF Bounds, int Index)> menuTargets = [];

    public int MenuItemAt(PointF point)
    {
        var local = new PointF((point.X - offsetX) / scale, (point.Y - offsetY) / scale);
        foreach (var target in menuTargets)
            if (target.Bounds.Contains(local)) return target.Index;
        return -1;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;
        var view = readView();
        canvas.FillColor = Paper;
        canvas.FillRectangle(dirtyRect);
        scale = Math.Min(dirtyRect.Width / 320f, dirtyRect.Height / 270f);
        offsetX = dirtyRect.X;
        offsetY = dirtyRect.Y;
        canvas.SaveState();
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale, scale);
        var w = dirtyRect.Width / scale;
        var h = dirtyRect.Height / scale;
        menuTargets.Clear();
        canvas.FillColor = Faint.WithAlpha(0.28f);
        for (var x = 5f; x < w; x += 6)
        for (var y = 5f; y < h; y += 6) canvas.FillRectangle(x, y, 0.7f, 0.7f);

        switch (view.Mode)
        {
            case ScreenMode.Menu: DrawMenu(canvas, w, h, view); break;
            case ScreenMode.Records: DrawRecords(canvas, w, h, view); break;
            case ScreenMode.Help: DrawHelp(canvas, w, h, view.Game.Definition); break;
            default:
                view.Game.Draw(canvas, new RectF(0, 0, w, h), new GameDrawContext(view.Best));
                if (view.Mode == ScreenMode.Paused) DrawOverlay(canvas, w, h, "PAUSA", "START PARA CONTINUAR", "SELECT PARA EL MENU");
                if (view.Mode == ScreenMode.GameOver) DrawOverlay(canvas, w, h, view.Game.Won ? "VICTORIA" : "FIN DEL JUEGO", "PUNTOS " + view.Game.Score, "START: OTRA PARTIDA");
                break;
        }
        canvas.RestoreState();
    }

    private void DrawMenu(ICanvas c, float w, float h, ConsoleViewState view)
    {
        Center(c, "HI " + view.Best.ToString("D6"), w, 16, 1.1f);
        var titleY = Math.Max(44, h * 0.15f);
        var logoX = w / 2 - 12;
        Block(c, logoX, titleY, 12, false);
        Block(c, logoX + 12, titleY, 12, false);
        Block(c, logoX - 12, titleY + 12, 12, false);
        Block(c, logoX, titleY + 12, 12, false);
        Center(c, "BLOCKDROP", w, titleY + 39, 3.6f);
        Center(c, "EDICION POCKET", w, titleY + 74, 1.1f);
        var top = Math.Max(titleY + 108, h * 0.46f);
        var visibleCount = Math.Min(4, view.MenuEntries.Count);
        if (visibleCount == 0) return;
        var first = Math.Clamp(view.Selection - visibleCount + 1, 0, view.MenuEntries.Count - visibleCount);
        var rowHeight = Math.Min(36, (h - top - 44) / visibleCount);
        for (var row = 0; row < visibleCount; row++)
        {
            var index = first + row;
            var rect = new RectF(w / 2 - 120, top + row * rowHeight, 240, rowHeight - 3);
            menuTargets.Add((rect, index));
            var selected = index == view.Selection;
            if (selected) { c.FillColor = Ink; c.FillRectangle(rect); }
            Text(c, selected ? ">" : " ", rect.X + 12, rect.Y + (rowHeight - 14) / 2, 1.6f, selected ? Paper : Ink);
            var label = view.MenuEntries[index].Label;
            var size = Math.Min(1.6f, 190f / Math.Max(1, label.Length * 6 - 1));
            Text(c, label, rect.X + 36, rect.Y + (rowHeight - 14) / 2, size, selected ? Paper : Ink);
        }
        if (view.MenuEntries.Count > visibleCount)
            Text(c, $"{view.Selection + 1}/{view.MenuEntries.Count}", w - 38, top - 15, 1f);
        Center(c, "CRUCETA: ELEGIR   A: ACEPTAR", w, h - 28, 1f);
        if (!Blink) Center(c, "PULSA START", w, h - 14, 1f);
    }

    private static void DrawRecords(ICanvas c, float w, float h, ConsoleViewState view)
    {
        Center(c, "RECORDS", w, h * 0.15f, 2.7f);
        Center(c, view.Game.Definition.Title, w, h * 0.25f, 1.1f);
        var top = h * 0.32f;
        if (view.Records.Count == 0)
        {
            Center(c, "TU HISTORIA EMPIEZA AQUI", w, top + 25, 1.3f);
            Center(c, "JUEGA TU PRIMERA PARTIDA", w, top + 49, 1.2f);
        }
        else for (var i = 0; i < view.Records.Count; i++)
            Center(c, $"{i + 1}.  {view.Records[i]:D6}", w, top + i * Math.Min(31, (h - top - 100) / view.Records.Count), 1.8f);
        Center(c, "MEJOR: " + view.Best.ToString("D6"), w, h - 62, 1.3f);
        Center(c, "B / SELECT: VOLVER", w, h - 27, 1f);
    }

    private static void DrawHelp(ICanvas c, float w, float h, GameDefinition game)
    {
        Center(c, "COMO JUGAR", w, h * 0.1f, 2.3f);
        Center(c, game.Title, w, h * 0.19f, 1.1f);
        var lines = game.HelpLines;
        var step = Math.Min(25, (h - h * 0.26f - 42) / Math.Max(1, lines.Count));
        for (var i = 0; i < lines.Count; i++) Center(c, lines[i], w, h * 0.26f + i * step, 1.15f);
        Center(c, "B / SELECT: VOLVER", w, h - 27, 1f);
    }

    private static void DrawOverlay(ICanvas c, float w, float h, string title, string middle, string footer)
    {
        var box = new RectF(w / 2 - 145, h / 2 - 58, 290, 116);
        c.FillColor = Paper;
        c.FillRectangle(box);
        c.StrokeColor = Ink;
        c.StrokeSize = 3;
        c.DrawRectangle(box);
        Center(c, title, w, box.Y + 19, title.Length > 9 ? 2f : 3f);
        Center(c, middle, w, box.Y + 59, 1.25f);
        Center(c, footer, w, box.Y + 87, 1.1f);
    }
}
