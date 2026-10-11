using BlockDrop.Shared.Contracts;
using Microsoft.Maui.Graphics;

namespace BlockDrop.Games.Racing;

public sealed class RacingModule : IRetroGame
{
    public static GameDefinition Metadata { get; } = new(
        "racing", "CARRITOS", "POCKET RACING / TRES CARRILES", "TURBO", "FRENO",
        ["IZQ / DER : CAMBIAR CARRIL", "ARRIBA / A: TURBO", "ABAJO / B : FRENAR",
         "START     : PAUSA", "SELECT    : MENU", "", "ESQUIVA EL TRAFICO",
         "TIENES TRES VIDAS", "EL TURBO GASTA COMBUSTIBLE"]);
    public RacingGame Engine { get; }
    private readonly RacingScreen screen;
    public GameDefinition Definition => Metadata;
    public bool Started => Engine.Started;
    public bool Paused => Engine.Paused;
    public bool GameOver => Engine.GameOver;
    public int Score => Engine.Score;
    public RacingModule(Random? random = null)
    {
        Engine = new RacingGame(random);
        screen = new RacingScreen(Engine);
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
            case GameButton.Up:
            case GameButton.A: Engine.Accelerate(); break;
            case GameButton.Down:
            case GameButton.B: Engine.Brake(); break;
        }
    }
    public void Draw(ICanvas canvas, RectF bounds, GameDrawContext context) => screen.Draw(canvas, bounds, context);
}
