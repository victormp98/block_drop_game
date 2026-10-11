using System.Reflection;
using System.Text.Json;
using BlockDrop.Games.Tetris;
using BlockDrop.Games.Snake;
using BlockDrop.Games.Racing;
using BlockDrop.Games.Platformer;
using BlockDrop.ConsoleUI;
using BlockDrop.Shared.Contracts;
using Microsoft.Maui.Graphics;

var module = new TetrisModule(new Random(42));
var game = module.Engine;
game.Start();
game.Move(-1);
game.Move(-1);
game.HardDrop();
game.Move(1);
game.Move(1);
game.HardDrop();
var snake = new SnakeModule(new Random(42));
snake.Start();
snake.HandleInput(GameButton.Up);
snake.Update(0.24);
var racing = new RacingModule(new Random(42));
racing.Start();
for (var frame = 0; frame < 90; frame++) racing.Update(1.0 / 60);
var platformer = new PlatformerModule();
platformer.Start();
platformer.HandleInput(GameButton.Right);
platformer.HandleInput(GameButton.A);
for (var frame = 0; frame < 60; frame++) platformer.Update(1.0 / 60);
platformer.ReleaseInput(GameButton.Right);
IRetroGame[] modules = [module, snake, racing, platformer];
MenuEntry[] entries = [.. modules.Select(item => new MenuEntry(item.Definition.Title, MenuAction.Play, item.Definition.Id)),
    new("RECORDS", MenuAction.Records), new("CONTROLES", MenuAction.Help)];
var view = new ConsoleViewState(ScreenMode.Menu, module, 1350, [1350, 900, 700, 300, 100], entries, 0);
var screen = new ConsoleScreen(() => view);
var canvas = DispatchProxy.Create<ICanvas, RecordingCanvas>();
var recorder = (RecordingCanvas)canvas;
var count = 0;
var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../artifacts/verification"));
Directory.CreateDirectory(output);
foreach (var active in modules)
foreach (var (width, height) in new (int, int)[] { (280, 260), (320, 320), (360, 480), (600, 600), (900, 380) })
foreach (var mode in Enum.GetValues<ScreenMode>())
{
    view = view with { Mode = mode, Game = active, Selection = 0 };
    recorder.Clear();
    screen.Draw(canvas, new RectF(0, 0, width, height));
    if (recorder.Operations.Any(op => op.X < -0.1f || op.Y < -0.1f || op.X + op.Width > width + 0.1f || op.Y + op.Height > height + 0.1f))
        throw new Exception($"Drawing outside the LCD in {active.Definition.Id}/{mode} at {width}x{height}");
    count++;
    if (width == 360)
        File.WriteAllText(Path.Combine(output, active.Definition.Id + "-" + mode.ToString().ToLowerInvariant() + "-lcd.json"),
            JsonSerializer.Serialize(new { width, height, operations = recorder.Operations }));
    if (mode == ScreenMode.Menu)
    {
        var hitOptions = new HashSet<int>();
        for (var y = 0; y < height; y++) hitOptions.Add(screen.MenuItemAt(new PointF(width / 2, y)));
        if (!Enumerable.Range(0, Math.Min(4, view.MenuEntries.Count)).All(hitOptions.Contains)) throw new Exception("Visible menu option cannot be touched");
    }
}
view = view with { Mode = ScreenMode.Menu, MenuEntries = [new("CONTINUAR", MenuAction.Resume), new("NUEVA PARTIDA", MenuAction.NewGame), new("RECORDS", MenuAction.Records), new("CONTROLES", MenuAction.Help)] };
recorder.Clear();
screen.Draw(canvas, new RectF(0, 0, 280, 260));
var resumedHits = new HashSet<int>();
for (var y = 0; y < 260; y++) resumedHits.Add(screen.MenuItemAt(new PointF(140, y)));
if (!Enumerable.Range(0, 4).All(resumedHits.Contains)) throw new Exception("Resume menu option cannot be touched");
Console.WriteLine($"All {count} LCD layouts and menu touch targets passed.");

var manyEntries = Enumerable.Range(0, 12).Select(index => new MenuEntry("JUEGO " + index, MenuAction.Play, "game-" + index)).ToArray();
view = view with { MenuEntries = manyEntries };
for (var selected = 0; selected < manyEntries.Length; selected++)
{
    view = view with { Selection = selected };
    recorder.Clear();
    screen.Draw(canvas, new RectF(0, 0, 280, 260));
    var visible = new HashSet<int>();
    for (var row = 0; row < 260; row++) visible.Add(screen.MenuItemAt(new PointF(140, row)));
    if (!visible.Contains(selected)) throw new Exception("Selected game scrolled outside the menu");
}
Console.WriteLine("All 12 additional-game menu scroll and touch checks passed.");

var flatMap = Enumerable.Range(0, 12).Select(_ => new string('.', 64).ToCharArray()).ToArray();
for (var column = 0; column < 64; column++) flatMap[10][column] = flatMap[11][column] = '#';
flatMap[9][2] = 'S';
flatMap[9][61] = 'F';
var scrolling = new PlatformerModule([new PlatformerLevel(flatMap.Select(row => new string(row)).ToArray())]);
scrolling.Start();
scrolling.HandleInput(GameButton.Right);
for (var frame = 0; frame < 300; frame++) scrolling.Update(1.0 / 60);
scrolling.HandleInput(GameButton.A);
for (var frame = 0; frame < 15; frame++) scrolling.Update(1.0 / 60);
scrolling.ReleaseInput(GameButton.Right);
var shortMap = new PlatformerLevel("..........", "..S.....F.", "##########");
var intro = new PlatformerModule([shortMap, shortMap]);
intro.Start();
intro.HandleInput(GameButton.Right);
for (var frame = 0; frame < 200 && intro.Engine.LevelIndex == 0; frame++) intro.Update(1.0 / 60);
var winner = new PlatformerModule([shortMap]);
winner.Start();
winner.HandleInput(GameButton.Right);
for (var frame = 0; frame < 200 && !winner.GameOver; frame++) winner.Update(1.0 / 60);
if (!winner.Won || intro.Engine.IntroTime <= 0 || scrolling.Engine.X < 20 || scrolling.Engine.Grounded)
    throw new Exception("Platformer drawing fixtures must contain real scrolling, airborne, level-transition and victory states");
var additionalLayouts = 0;
foreach (var (variant, active, mode) in new (string, IRetroGame, ScreenMode)[]
    { ("scrolling", scrolling, ScreenMode.Playing), ("intro", intro, ScreenMode.Playing), ("victory", winner, ScreenMode.GameOver) })
foreach (var (width, height) in new (int, int)[] { (280, 260), (320, 320), (360, 480), (600, 600), (900, 380) })
{
    view = view with { Game = active, Mode = mode };
    recorder.Clear();
    screen.Draw(canvas, new RectF(0, 0, width, height));
    if (recorder.Operations.Any(op => op.X < -0.1f || op.Y < -0.1f || op.X + op.Width > width + 0.1f || op.Y + op.Height > height + 0.1f))
        throw new Exception($"Platformer {variant} escaped the LCD at {width}x{height}");
    if (width == 360)
        File.WriteAllText(Path.Combine(output, "platformer-" + variant + "-lcd.json"),
            JsonSerializer.Serialize(new { width, height, operations = recorder.Operations }));
    additionalLayouts++;
}
Console.WriteLine($"All {additionalLayouts} additional platformer scrolling, transition and victory layouts passed.");

public class RecordingCanvas : DispatchProxy
{
    public record DrawOperation(float X, float Y, float Width, float Height, string Color, float Stroke);
    public List<DrawOperation> Operations { get; } = [];
    private Color fill = Colors.Black;
    private Color stroke = Colors.Black;
    private float strokeSize = 1;
    private float scaleX = 1, scaleY = 1, x, y;
    private readonly Stack<(float, float, float, float)> transforms = new();
    public void Clear()
    {
        Operations.Clear();
        transforms.Clear();
        scaleX = scaleY = 1;
        x = y = 0;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        args ??= [];
        switch (method!.Name)
        {
            case "set_FillColor": fill = (Color)args[0]!; break;
            case "set_StrokeColor": stroke = (Color)args[0]!; break;
            case "set_StrokeSize": strokeSize = Convert.ToSingle(args[0]); break;
            case "SaveState": transforms.Push((scaleX, scaleY, x, y)); break;
            case "RestoreState": (scaleX, scaleY, x, y) = transforms.Pop(); return true;
            case "Translate": x += Convert.ToSingle(args[0]) * scaleX; y += Convert.ToSingle(args[1]) * scaleY; break;
            case "Scale": scaleX *= Convert.ToSingle(args[0]); scaleY *= Convert.ToSingle(args[1]); break;
            case "FillRectangle":
            case "DrawRectangle":
                var rect = args.Length == 1 ? (RectF)args[0]! : new RectF(Convert.ToSingle(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2]), Convert.ToSingle(args[3]));
                var isFill = method.Name == "FillRectangle";
                Operations.Add(new DrawOperation(x + rect.X * scaleX, y + rect.Y * scaleY, rect.Width * scaleX, rect.Height * scaleY,
                    (isFill ? fill : stroke).ToRgbaHex(), isFill ? 0 : strokeSize * scaleX));
                break;
        }
        return method.ReturnType == typeof(void) ? null : method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
    }
}
