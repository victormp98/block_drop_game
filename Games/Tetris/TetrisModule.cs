using BlockDrop.Shared.Contracts;
using Microsoft.Maui.Graphics;

namespace BlockDrop.Games.Tetris;

public sealed class TetrisModule : IRetroGame
{
    public static GameDefinition Metadata { get; } = new(
        "tetris", "TETRIS", "10 × 20   /   CLASSIC BLOCK SYSTEM", "GIRAR", "SOLTAR",
        ["IZQ / DER : MOVER", "ABAJO     : BAJAR", "ARRIBA / A: GIRAR", "B         : SOLTAR",
         "START     : PAUSA", "SELECT    : MENU", "", "COMPLETA FILAS PARA SUMAR", "CADA 10 LINEAS SUBE EL NIVEL"],
        LegacyScoreFile: "scores.json");

    public TetrisGame Engine { get; }
    private readonly TetrisScreen screen;
    public GameDefinition Definition => Metadata;
    public bool Started => Engine.Started;
    public bool Paused => Engine.Paused;
    public bool GameOver => Engine.GameOver;
    public int Score => Engine.Score;

    public TetrisModule(Random? random = null)
    {
        Engine = new TetrisGame(random);
        screen = new TetrisScreen(Engine);
    }

    public void Start() => Engine.Start();
    public void SetPaused(bool paused) => Engine.SetPaused(paused);
    public void Update(double seconds) => Engine.Tick(seconds);
    public void HandleInput(GameButton button)
    {
        switch (button)
        {
            case GameButton.Left: Engine.Move(-1); break;
            case GameButton.Right: Engine.Move(1); break;
            case GameButton.Down: Engine.SoftDrop(); break;
            case GameButton.Up:
            case GameButton.A: Engine.Rotate(); break;
            case GameButton.B: Engine.HardDrop(); break;
        }
    }

    public void Draw(ICanvas canvas, RectF bounds, GameDrawContext context) => screen.Draw(canvas, bounds, context);
}
