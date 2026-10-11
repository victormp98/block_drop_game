namespace BlockDrop.Games.Platformer;

public readonly record struct WorldPoint(double X, double Y);
public readonly record struct EnemySpawn(double X, double Y, double Left, double Right);

public sealed class PlatformerLevel
{
    private readonly string[] rows;
    public int Width => rows[0].Length;
    public int Height => rows.Length;
    public WorldPoint Spawn { get; }
    public WorldPoint Goal { get; }
    public IReadOnlyList<WorldPoint> Coins { get; }
    public IReadOnlyList<WorldPoint> Checkpoints { get; }
    public IReadOnlyList<EnemySpawn> Enemies { get; }

    // # terrain, S start, F finish, C coin, E enemy, K checkpoint.
    public PlatformerLevel(params string[] map)
    {
        if (map.Length < 3 || map[0].Length < 5 || map.Any(row => row.Length != map[0].Length))
            throw new ArgumentException("A level must have equally sized rows.", nameof(map));
        rows = (string[])map.Clone();
        var coins = new List<WorldPoint>();
        var checkpoints = new List<WorldPoint>();
        var enemies = new List<EnemySpawn>();
        WorldPoint? spawn = null, goal = null;
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            switch (rows[y][x])
            {
                case 'S':
                    if (spawn is not null) throw new ArgumentException("Duplicate level start.", nameof(map));
                    spawn = new(x + 0.15, y + 0.1);
                    break;
                case 'F':
                    if (goal is not null) throw new ArgumentException("Duplicate level finish.", nameof(map));
                    goal = new(x, y);
                    break;
                case 'C': coins.Add(new(x + 0.25, y + 0.25)); break;
                case 'K': checkpoints.Add(new(x + 0.15, y + 0.1)); break;
                case 'E':
                    var left = x;
                    var right = x;
                    while (left > 0 && SolidAt(left - 1, y + 1)) left--;
                    while (right < Width - 1 && SolidAt(right + 1, y + 1)) right++;
                    // Keep patrols local so enemies cannot wander into the starting area.
                    enemies.Add(new(x + 0.1, y + 0.3, Math.Max(left, x - 2), Math.Min(right + 0.25, x + 2)));
                    break;
                case '.': case '#': break;
                default: throw new ArgumentException("Unknown level tile.", nameof(map));
            }
        }
        Spawn = spawn ?? throw new ArgumentException("Missing level start.", nameof(map));
        Goal = goal ?? throw new ArgumentException("Missing level finish.", nameof(map));
        Coins = coins.AsReadOnly();
        Checkpoints = checkpoints.AsReadOnly();
        Enemies = enemies.AsReadOnly();
    }

    public bool SolidAt(int x, int y) => x < 0 || x >= Width || y < 0 || (y < Height && rows[y][x] == '#');
}

public static class PlatformerLevels
{
    public static IReadOnlyList<PlatformerLevel> Create() => new[]
    {
        Build(48, [(14, 2), (32, 2)], [(7, 7, 4), (23, 7, 4), (39, 7, 3)],
            [5, 10, 19, 28, 36, 43], [8, 24, 40], [21, 37]),
        Build(56, [(12, 2), (29, 3), (44, 2)], [(5, 7, 4), (20, 7, 4), (36, 7, 4)],
            [4, 9, 18, 26, 34, 42, 50], [7, 23, 39, 49], [19, 35, 48]),
        Build(64, [(15, 3), (33, 3), (50, 3)], [(7, 7, 4), (25, 7, 4), (42, 7, 4), (56, 7, 4)],
            [5, 12, 22, 30, 39, 48, 55, 60], [10, 28, 45, 58], [21, 38, 55])
    };

    private static PlatformerLevel Build(int width, (int X, int Length)[] gaps, (int X, int Y, int Length)[] ledges,
        int[] coinColumns, int[] enemies, int[] checkpoints)
    {
        var map = Enumerable.Range(0, 12).Select(_ => Enumerable.Repeat('.', width).ToArray()).ToArray();
        for (var x = 0; x < width; x++) map[10][x] = map[11][x] = '#';
        foreach (var (x, length) in gaps)
            for (var column = x; column < x + length; column++) map[10][column] = map[11][column] = '.';
        foreach (var (x, y, length) in ledges)
        {
            for (var column = x; column < x + length; column++) map[y][column] = '#';
            map[y - 1][x + 1] = 'C';
            map[y - 1][x + 2] = 'C';
        }
        foreach (var x in coinColumns) map[9][x] = 'C';
        foreach (var x in enemies) map[9][x] = 'E';
        foreach (var x in checkpoints) map[9][x] = 'K';
        map[9][2] = 'S';
        map[9][width - 3] = 'F';
        return new PlatformerLevel(map.Select(row => new string(row)).ToArray());
    }
}
