namespace BlockDrop.Games.Platformer;

public sealed class PlatformCoin(WorldPoint position)
{
    public WorldPoint Position { get; } = position;
    public bool Collected { get; internal set; }
}

public sealed class PlatformEnemy(EnemySpawn spawn)
{
    public double X { get; internal set; } = spawn.X;
    public double Y { get; } = spawn.Y;
    public double Left { get; } = spawn.Left;
    public double Right { get; } = spawn.Right;
    public int Direction { get; internal set; } = -1;
    public bool Alive { get; internal set; } = true;
}

public sealed class PlatformerGame
{
    public const double PlayerWidth = 0.65;
    public const double PlayerHeight = 0.9;
    private readonly IReadOnlyList<PlatformerLevel> levels;
    private int direction;
    private double jumpBuffer;
    private double coyote;
    private double invincible;
    private WorldPoint checkpoint;
    public bool Started { get; private set; }
    public bool Paused { get; private set; }
    public bool GameOver { get; private set; }
    public bool Won { get; private set; }
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int LevelIndex { get; private set; }
    public int LevelCount => levels.Count;
    public PlatformerLevel Level => levels[LevelIndex];
    public IReadOnlyList<PlatformCoin> Coins { get; private set; } = [];
    public IReadOnlyList<PlatformEnemy> Enemies { get; private set; } = [];
    public double X { get; private set; }
    public double Y { get; private set; }
    public double VelocityY { get; private set; }
    public bool Grounded { get; private set; }
    public bool Running { get; private set; }
    public bool FacingLeft { get; private set; }
    public double Invincible => invincible;
    public double IntroTime { get; private set; }
    public double Time { get; private set; }
    public WorldPoint Checkpoint => checkpoint;

    public PlatformerGame(IReadOnlyList<PlatformerLevel>? levels = null)
    {
        this.levels = (levels ?? PlatformerLevels.Create()).ToArray();
        if (this.levels.Count == 0) throw new ArgumentException("At least one level is required.", nameof(levels));
    }

    public void Start()
    {
        Started = true;
        Paused = GameOver = Won = Running = FacingLeft = false;
        Score = LevelIndex = 0;
        Lives = 3;
        Time = 0;
        LoadLevel();
        IntroTime = 0;
    }

    public void SetPaused(bool paused)
    {
        Paused = paused;
        if (paused) { direction = 0; jumpBuffer = 0; }
    }
    public void Move(int value)
    {
        if (!Started || Paused || GameOver) return;
        direction = Math.Sign(value);
        if (direction != 0) FacingLeft = direction < 0;
    }
    public void Release(int value)
    {
        if (direction == Math.Sign(value)) direction = 0;
    }
    public void Jump()
    {
        if (Started && !Paused && !GameOver) jumpBuffer = 0.15;
    }
    public void ToggleRun()
    {
        if (Started && !Paused && !GameOver) Running = !Running;
    }

    public void Tick(double seconds)
    {
        if (!Started || Paused || GameOver || !double.IsFinite(seconds) || seconds <= 0) return;
        var remaining = Math.Min(seconds, 0.25);
        while (remaining > 0 && !GameOver)
        {
            var dt = Math.Min(remaining, 1.0 / 120);
            remaining -= dt;
            Time += dt;
            if (IntroTime > 0) { IntroTime = Math.Max(0, IntroTime - dt); continue; }
            Step(dt);
        }
    }

    private void LoadLevel()
    {
        Coins = Level.Coins.Select(point => new PlatformCoin(point)).ToArray();
        Enemies = Level.Enemies.Select(spawn => new PlatformEnemy(spawn)).ToArray();
        checkpoint = Level.Spawn;
        Respawn();
        invincible = 0;
    }

    private void Respawn()
    {
        X = checkpoint.X;
        Y = checkpoint.Y;
        VelocityY = 0;
        direction = 0;
        jumpBuffer = coyote = 0;
        Grounded = true;
        invincible = 1.5;
    }

    private void Step(double dt)
    {
        invincible = Math.Max(0, invincible - dt);
        coyote = Grounded ? 0.1 : Math.Max(0, coyote - dt);
        if (jumpBuffer > 0 && coyote > 0)
        {
            VelocityY = -12.2;
            Grounded = false;
            coyote = jumpBuffer = 0;
        }
        jumpBuffer = Math.Max(0, jumpBuffer - dt);
        var previousBottom = Y + PlayerHeight;
        MoveHorizontal(direction * (Running ? 6.2 : 4.2) * dt);
        VelocityY = Math.Min(18, VelocityY + 23 * dt);
        MoveVertical(VelocityY * dt);
        if (Y > Level.Height + 1) { Die(); return; }

        foreach (var coin in Coins)
            if (!coin.Collected && Overlaps(X, Y, PlayerWidth, PlayerHeight, coin.Position.X, coin.Position.Y, 0.5, 0.5))
            { coin.Collected = true; Score += 100; }

        foreach (var enemy in Enemies.Where(enemy => enemy.Alive))
        {
            enemy.X += enemy.Direction * (1.15 + LevelIndex * 0.15) * dt;
            if (enemy.X < enemy.Left) { enemy.X = enemy.Left; enemy.Direction = 1; }
            if (enemy.X > enemy.Right) { enemy.X = enemy.Right; enemy.Direction = -1; }
            if (!Overlaps(X, Y, PlayerWidth, PlayerHeight, enemy.X, enemy.Y, 0.75, 0.7)) continue;
            if (VelocityY > 0 && previousBottom <= enemy.Y + 0.08)
            {
                enemy.Alive = false;
                Score += 200;
                VelocityY = -7;
                Grounded = false;
            }
            else if (invincible <= 0) { Die(); return; }
        }

        foreach (var point in Level.Checkpoints)
            if (Grounded && X >= point.X && point.X > checkpoint.X) checkpoint = point;

        if (Overlaps(X, Y, PlayerWidth, PlayerHeight, Level.Goal.X, Level.Goal.Y - 1, 1, 2))
        {
            Score += 1000;
            if (LevelIndex == levels.Count - 1) { Score += Lives * 200; Won = GameOver = true; direction = 0; }
            else { LevelIndex++; LoadLevel(); IntroTime = 0.8; }
        }
    }

    private void MoveHorizontal(double dx)
    {
        if (dx == 0) return;
        X = Math.Clamp(X + dx, 0, Level.Width - PlayerWidth);
        foreach (var (tx, ty) in TouchingTiles())
            if (Level.SolidAt(tx, ty)) X = dx > 0 ? tx - PlayerWidth : tx + 1;
    }

    private void MoveVertical(double dy)
    {
        Y += dy;
        Grounded = false;
        foreach (var (tx, ty) in TouchingTiles())
        {
            if (!Level.SolidAt(tx, ty)) continue;
            if (dy > 0) { Y = ty - PlayerHeight; Grounded = true; }
            else if (dy < 0) Y = ty + 1;
            VelocityY = 0;
        }
    }

    private IEnumerable<(int X, int Y)> TouchingTiles()
    {
        for (var y = (int)Math.Floor(Y + 0.00001); y <= (int)Math.Floor(Y + PlayerHeight - 0.00001); y++)
        for (var x = (int)Math.Floor(X + 0.00001); x <= (int)Math.Floor(X + PlayerWidth - 0.00001); x++)
            yield return (x, y);
    }

    private void Die()
    {
        Lives--;
        if (Lives <= 0) { Lives = 0; GameOver = true; direction = 0; }
        else Respawn();
    }

    private static bool Overlaps(double x, double y, double w, double h, double bx, double by, double bw, double bh) =>
        x < bx + bw && x + w > bx && y < by + bh && y + h > by;
}
