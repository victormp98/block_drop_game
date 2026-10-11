using BlockDrop.Games.Tetris;

namespace BlockDrop.Tests.Tetris;

public static class TetrisChecks
{
    public static int Run()
    {
        var checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks++;
            Console.WriteLine("PASS " + name);
        }
        
        TetrisGame WithPiece(int piece)
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var result = new TetrisGame(new Random(seed));
                result.Start();
                if (result.Piece == piece) return result;
            }
            throw new Exception("Could not obtain piece " + piece);
        }
        
        int Filled(TetrisGame game) => game.Board.Cast<int>().Count(value => value > 0);
        
        for (var piece = 0; piece < 7; piece++)
        for (var rotation = 0; rotation < 4; rotation++)
            Check(TetrisGame.PieceCells(piece, rotation).Distinct().Count() == 4, $"Four cells in piece {piece}, rotation {rotation}");
        
        var game = new TetrisGame(new Random(42));
        game.Start();
        Check(game.Active && Filled(game) == 0 && game.Score == 0 && game.Level == 1, "New game resets board and score");
        var bag = new HashSet<int>();
        for (var n = 0; n < 7; n++)
        {
            bag.Add(game.Piece);
            game.HardDrop();
            Array.Clear(game.Board);
        }
        Check(bag.Count == 7, "Seven piece bag has no missing or repeated piece");
        
        game = WithPiece(2);
        while (game.Move(-1)) { }
        Check(TetrisGame.PieceCells(game.Piece, game.Rotation).Min(cell => game.X + cell.X) == 0 && !game.Move(-1), "Left wall stops movement");
        while (game.Move(1)) { }
        Check(TetrisGame.PieceCells(game.Piece, game.Rotation).Max(cell => game.X + cell.X) == 9 && !game.Move(1), "Right wall stops movement");
        var original = TetrisGame.PieceCells(game.Piece, game.Rotation).OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
        for (var turn = 0; turn < 4; turn++) game.Rotate();
        Check(original.SequenceEqual(TetrisGame.PieceCells(game.Piece, game.Rotation).OrderBy(cell => cell.Y).ThenBy(cell => cell.X)), "Four rotations restore the shape");
        
        game = WithPiece(0);
        game.Rotate();
        while (game.Move(-1)) { }
        Check(game.Rotate() && TetrisGame.PieceCells(game.Piece, game.Rotation).All(cell => game.X + cell.X >= 0), "Rotation kicks away from a wall");
        
        game = WithPiece(2);
        var initialY = game.Y;
        for (var n = 0; n < 4; n++) game.Tick(0.21);
        Check(game.Y == initialY + 1, "Gravity moves the piece after its interval");
        var frozen = (game.X, game.Y, game.Rotation, game.Score);
        game.SetPaused(true);
        game.Move(-1); game.Rotate(); game.SoftDrop(); game.HardDrop(); game.Tick(0.25);
        Check(frozen == (game.X, game.Y, game.Rotation, game.Score) && Filled(game) == 0, "Pause freezes all gameplay");
        game.SetPaused(false);
        Check(game.SoftDrop() && game.Score == 1, "Soft drop awards one point");
        var ghost = game.GhostY;
        var expectedCells = TetrisGame.PieceCells(game.Piece, game.Rotation).Select(cell => new Cell(game.X + cell.X, ghost + cell.Y)).ToArray();
        var expectedScore = game.Score + (ghost - game.Y) * 2;
        game.HardDrop();
        Check(expectedCells.All(cell => game.Board[cell.Y, cell.X] != 0) && Filled(game) == 4 && game.Score == expectedScore, "Hard drop lands at ghost and scores distance");
        
        game = WithPiece(1);
        while (game.SoftDrop()) { }
        game.Tick(0.2);
        Check(Filled(game) == 0, "Grounded piece allows time to adjust");
        game.Tick(0.25);
        Check(Filled(game) == 4, "Grounded piece locks after delay");
        
        for (var count = 1; count <= 4; count++)
        {
            game = WithPiece(0);
            game.Rotate();
            var gap = game.X + TetrisGame.PieceCells(game.Piece, game.Rotation).First().X;
            for (var row = TetrisGame.Rows - count; row < TetrisGame.Rows; row++)
            for (var column = 0; column < TetrisGame.Columns; column++) game.Board[row, column] = column == gap ? 0 : 2;
            game.Board[TetrisGame.Rows - count - 1, 0] = 7;
            var dropScore = (game.GhostY - game.Y) * 2;
            game.HardDrop();
            var lineScore = count switch { 1 => 100, 2 => 300, 3 => 500, _ => 800 };
            Check(game.Lines == count && game.LastClear == count && game.Score == lineScore + dropScore, $"Clearing {count} lines gives correct score");
            Check(game.Board[19, 0] == 7, $"Rows compact correctly after clearing {count} lines");
        }
        
        game = WithPiece(0);
        for (var cycle = 0; cycle < 3; cycle++)
        {
            while (game.Piece != 0)
            {
                game.HardDrop();
                Array.Clear(game.Board);
            }
            game.Rotate();
            var gap = game.X + TetrisGame.PieceCells(game.Piece, game.Rotation).First().X;
            for (var row = 16; row < 20; row++)
            for (var column = 0; column < 10; column++) game.Board[row, column] = column == gap ? 0 : 1;
            game.HardDrop();
            Array.Clear(game.Board);
        }
        Check(game.Lines == 12 && game.Level == 2 && game.FallInterval < 0.8, "Every ten lines increases level and speed");
        
        game = WithPiece(1);
        for (var row = 0; row < 3; row++)
        for (var column = 1; column < 10; column++) game.Board[row, column] = 1;
        // Free only the current piece so it can lock, then block the next spawn.
        foreach (var cell in TetrisGame.PieceCells(game.Piece)) game.Board[game.Y + cell.Y, game.X + cell.X] = 0;
        game.HardDrop();
        Check(game.GameOver && !game.Active, "Blocked spawn ends the game");
        var finalScore = game.Score;
        game.HardDrop(); game.SoftDrop(); game.Move(1); game.Rotate();
        Check(game.Score == finalScore, "Game over ignores movement and drops");
        game.Start();
        Check(game.Active && game.Score == 0 && game.Lines == 0 && Filled(game) == 0, "Restart clears a finished game");
        
        return checks;
    }
}
