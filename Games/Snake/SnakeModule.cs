using BlockDrop.Shared.Contracts;
using Microsoft.Maui.Graphics;

namespace BlockDrop.Games.Snake;

public sealed class SnakeModule : IRetroGame
{
    public static GameDefinition Metadata { get; } = new(
        "snake", "GUSANITO", "SNAKE / COME Y CRECE", "DER", "IZQ",
        ["CRUCETA   : DIRECCION", "A         : GIRAR A LA DER", "B         : GIRAR A LA IZQ",
         "START     : PAUSA", "SELECT    : MENU", "", "COME PARA CRECER Y SUMAR",
         "EVITA PAREDES Y TU COLA", "CADA COMIDA SUBE LA VELOCIDAD"]);
    public SnakeGame Engine { get; }
    private readonly SnakeScreen screen;
    public GameDefinition Definition => Metadata;
    public bool Started => Engine.Started;
    public bool Paused => Engine.Paused;
    public bool GameOver => Engine.GameOver;
    public bool Won => Engine.Won;
    public int Score => Engine.Score;
    public SnakeModule(Random? random = null)
    {
        Engine = new SnakeGame(random);
        screen = new SnakeScreen(Engine);
    }
    public void Start() => Engine.Start();
    public void SetPaused(bool paused) => Engine.SetPaused(paused);
    public void Update(double seconds) => Engine.Tick(seconds);
    public void HandleInput(GameButton button)
    {
        switch (button)
        {
            case GameButton.Left: Engine.Turn(SnakeDirection.Left); break;
            case GameButton.Right: Engine.Turn(SnakeDirection.Right); break;
            case GameButton.Down: Engine.Turn(SnakeDirection.Down); break;
            case GameButton.Up: Engine.Turn(SnakeDirection.Up); break;
            case GameButton.A: Engine.TurnRelative(1); break;
            case GameButton.B: Engine.TurnRelative(-1); break;
        }
    }
    public void Draw(ICanvas canvas, RectF bounds, GameDrawContext context) => screen.Draw(canvas, bounds, context);
}
