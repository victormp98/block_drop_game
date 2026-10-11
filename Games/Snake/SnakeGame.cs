namespace BlockDrop.Games.Snake;

public enum SnakeDirection { Right, Down, Left, Up }
public readonly record struct SnakeCell(int X, int Y);

public sealed class SnakeGame
{
    public const int Columns = 20;
    public const int Rows = 18;
    private readonly Random random;
    private readonly List<SnakeCell> body = [];
    private readonly IReadOnlyList<SnakeCell> bodyView;
    private SnakeDirection? queuedDirection;
    private double elapsed;
    public IReadOnlyList<SnakeCell> Body => bodyView;
    public SnakeCell Food { get; private set; }
    public SnakeDirection Direction { get; private set; }
    public bool Started { get; private set; }
    public bool Paused { get; private set; }
    public bool GameOver { get; private set; }
    public bool Won { get; private set; }
    public int Score { get; private set; }
    public int Foods => Score / 100;
    public double StepSeconds => Math.Max(0.075, 0.22 - Foods * 0.006);

    public SnakeGame(Random? random = null)
    {
        this.random = random ?? new Random();
        bodyView = body.AsReadOnly();
    }

    public void Start()
    {
        Started = true;
        Paused = GameOver = Won = false;
        Score = 0;
        elapsed = 0;
        queuedDirection = null;
        Direction = SnakeDirection.Right;
        body.Clear();
        body.AddRange([new(8, 9), new(7, 9), new(6, 9), new(5, 9)]);
        PlaceFood();
    }

    public void SetPaused(bool paused) => Paused = paused;

    public void Turn(SnakeDirection direction)
    {
        if (!Started || Paused || GameOver || queuedDirection is not null) return;
        if (direction == Direction || ((int)direction + 2) % 4 == (int)Direction) return;
        queuedDirection = direction;
    }

    public void TurnRelative(int turn) => Turn((SnakeDirection)(((int)Direction + turn + 4) % 4));

    public void Tick(double seconds)
    {
        if (!Started || Paused || GameOver || !double.IsFinite(seconds) || seconds <= 0) return;
        elapsed += Math.Min(seconds, 0.25);
        while (!GameOver && elapsed >= StepSeconds)
        {
            elapsed -= StepSeconds;
            Step();
        }
    }

    private void Step()
    {
        Direction = queuedDirection ?? Direction;
        queuedDirection = null;
        var delta = Direction switch
        {
            SnakeDirection.Right => new SnakeCell(1, 0), SnakeDirection.Down => new SnakeCell(0, 1),
            SnakeDirection.Left => new SnakeCell(-1, 0), _ => new SnakeCell(0, -1)
        };
        var head = new SnakeCell(body[0].X + delta.X, body[0].Y + delta.Y);
        var eating = head == Food;
        // The tail vacates its cell during a normal step and is safe to enter.
        if (head.X < 0 || head.X >= Columns || head.Y < 0 || head.Y >= Rows ||
            body.Take(body.Count - (eating ? 0 : 1)).Contains(head))
        {
            GameOver = true;
            return;
        }
        body.Insert(0, head);
        if (!eating) body.RemoveAt(body.Count - 1);
        else
        {
            Score += 100;
            if (body.Count == Columns * Rows) { Won = GameOver = true; return; }
            PlaceFood();
        }
    }

    private void PlaceFood()
    {
        var empty = new List<SnakeCell>();
        for (var y = 0; y < Rows; y++)
        for (var x = 0; x < Columns; x++)
        {
            var cell = new SnakeCell(x, y);
            if (!body.Contains(cell)) empty.Add(cell);
        }
        Food = empty[random.Next(empty.Count)];
    }
}
