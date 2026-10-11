namespace BlockDrop.Games.Tetris;

public readonly record struct Cell(int X, int Y);

// The rules are independent of the UI so the same game runs on every platform.
public sealed class TetrisGame
{
    public const int Columns = 10;
    public const int Rows = 20;
    private static readonly string[][] Shapes =
    [
        ["0000", "1111", "0000", "0000"],
        ["11", "11"],
        ["010", "111", "000"],
        ["011", "110", "000"],
        ["110", "011", "000"],
        ["100", "111", "000"],
        ["001", "111", "000"]
    ];
    private static readonly Cell[] Kicks =
    [new(0, 0), new(-1, 0), new(1, 0), new(-2, 0), new(2, 0), new(0, -1), new(-1, -1), new(1, -1), new(0, -2)];
    private readonly Random random;
    private readonly Queue<int> bag = new();
    private double fallTime;
    private double lockTime;
    private int lockResets;

    public int[,] Board { get; } = new int[Rows, Columns];
    public int Piece { get; private set; }
    public int NextPiece { get; private set; }
    public int Rotation { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Score { get; private set; }
    public int Lines { get; private set; }
    public int Level => Lines / 10 + 1;
    public int LastClear { get; private set; }
    public bool Started { get; private set; }
    public bool Paused { get; private set; }
    public bool GameOver { get; private set; }
    public bool Active => Started && !Paused && !GameOver;
    public double FallInterval => Math.Max(0.075, 0.8 * Math.Pow(0.8, Level - 1));
    public int GhostY
    {
        get
        {
            var row = Y;
            while (CanPlace(Piece, Rotation, X, row + 1)) row++;
            return row;
        }
    }

    public TetrisGame(Random? random = null) => this.random = random ?? new Random();

    public void Start()
    {
        Array.Clear(Board);
        bag.Clear();
        Score = Lines = LastClear = 0;
        Started = true;
        Paused = GameOver = false;
        NextPiece = TakePiece();
        Spawn();
    }

    public void SetPaused(bool paused)
    {
        if (Started && !GameOver) Paused = paused;
    }

    public static IEnumerable<Cell> PieceCells(int piece, int rotation = 0)
    {
        var shape = Shapes[piece];
        var size = shape.Length;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            if (shape[y][x] != '1') continue;
            var cell = new Cell(x, y);
            for (var turn = 0; turn < (rotation & 3); turn++)
                cell = new Cell(size - 1 - cell.Y, cell.X);
            yield return cell;
        }
    }

    public bool Move(int direction)
    {
        if (!Active || direction is not (-1 or 1)) return false;
        var grounded = !CanPlace(Piece, Rotation, X, Y + 1);
        if (!CanPlace(Piece, Rotation, X + direction, Y)) return false;
        X += direction;
        ResetLock(grounded);
        return true;
    }

    public bool Rotate()
    {
        if (!Active || Piece == 1) return false;
        var grounded = !CanPlace(Piece, Rotation, X, Y + 1);
        var turn = (Rotation + 1) & 3;
        foreach (var kick in Kicks)
        {
            if (!CanPlace(Piece, turn, X + kick.X, Y + kick.Y)) continue;
            Rotation = turn;
            X += kick.X;
            Y += kick.Y;
            ResetLock(grounded);
            return true;
        }
        return false;
    }

    public bool SoftDrop()
    {
        if (!Active || !CanPlace(Piece, Rotation, X, Y + 1)) return false;
        Y++;
        Score++;
        fallTime = lockTime = 0;
        return true;
    }

    public void HardDrop()
    {
        if (!Active) return;
        var landing = GhostY;
        Score += (landing - Y) * 2;
        Y = landing;
        LockPiece();
    }

    public void Tick(double seconds)
    {
        if (!Active || seconds <= 0) return;
        seconds = Math.Min(seconds, 0.25);
        fallTime += seconds;
        while (fallTime >= FallInterval)
        {
            fallTime -= FallInterval;
            if (CanPlace(Piece, Rotation, X, Y + 1)) Y++;
        }
        if (CanPlace(Piece, Rotation, X, Y + 1)) lockTime = 0;
        else
        {
            lockTime += seconds;
            if (lockTime >= 0.45) LockPiece();
        }
    }

    private bool CanPlace(int piece, int rotation, int x, int y)
    {
        foreach (var cell in PieceCells(piece, rotation))
        {
            var column = x + cell.X;
            var row = y + cell.Y;
            if (column < 0 || column >= Columns || row >= Rows || row < -4) return false;
            if (row >= 0 && Board[row, column] != 0) return false;
        }
        return true;
    }

    private void ResetLock(bool grounded)
    {
        if (grounded && lockResets < 15)
        {
            lockTime = 0;
            lockResets++;
        }
    }

    private void LockPiece()
    {
        var cells = PieceCells(Piece, Rotation).Select(cell => new Cell(X + cell.X, Y + cell.Y)).ToArray();
        if (cells.Any(cell => cell.Y < 0))
        {
            GameOver = true;
            return;
        }
        foreach (var cell in cells) Board[cell.Y, cell.X] = Piece + 1;

        var cleared = 0;
        var destination = Rows - 1;
        for (var row = Rows - 1; row >= 0; row--)
        {
            var full = true;
            for (var column = 0; column < Columns; column++)
                if (Board[row, column] == 0) full = false;
            if (full) { cleared++; continue; }
            for (var column = 0; column < Columns; column++)
                Board[destination, column] = Board[row, column];
            destination--;
        }
        for (var row = destination; row >= 0; row--)
        for (var column = 0; column < Columns; column++) Board[row, column] = 0;

        LastClear = cleared;
        Score += (cleared switch { 1 => 100, 2 => 300, 3 => 500, 4 => 800, _ => 0 }) * Level;
        Lines += cleared;
        Spawn();
    }

    private void Spawn()
    {
        Piece = NextPiece;
        NextPiece = TakePiece();
        Rotation = 0;
        X = (Columns - Shapes[Piece].Length) / 2;
        Y = Piece == 0 ? -1 : 0;
        fallTime = lockTime = 0;
        lockResets = 0;
        if (!CanPlace(Piece, Rotation, X, Y)) GameOver = true;
    }

    private int TakePiece()
    {
        if (bag.Count == 0)
        {
            var pieces = Enumerable.Range(0, Shapes.Length).ToArray();
            random.Shuffle(pieces);
            foreach (var piece in pieces) bag.Enqueue(piece);
        }
        return bag.Dequeue();
    }
}
