using BlockDrop.Shared.Contracts;
using Microsoft.Maui.Graphics;

namespace BlockDrop.Games.Platformer;

public sealed class PlatformerModule : IRetroGame
{
    public static GameDefinition Metadata { get; } = new(
        "platformer", "POCKET RUN", "POCKET RUN / AVENTURA DE PIXELES", "SALTAR", "CORRER",
        ["IZQ / DER : CAMINAR", "ARRIBA / A: SALTAR", "B         : CORRER / CAMINAR",
         "START     : PAUSA", "SELECT    : MENU", "", "MONEDAS Y ENEMIGOS: PUNTOS",
         "SALTA ENCIMA DE LOS ENEMIGOS", "EVITA LOS HUECOS DEL SUELO", "LLEGA A LA BANDERA: 3 NIVELES"]);
    public PlatformerGame Engine { get; }
    private readonly PlatformerScreen screen;
    public GameDefinition Definition => Metadata;
    public bool Started => Engine.Started;
    public bool Paused => Engine.Paused;
    public bool GameOver => Engine.GameOver;
    public bool Won => Engine.Won;
    public int Score => Engine.Score;
    public PlatformerModule(IReadOnlyList<PlatformerLevel>? levels = null)
    {
        Engine = new PlatformerGame(levels);
        screen = new PlatformerScreen(Engine);
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
            case GameButton.A: Engine.Jump(); break;
            case GameButton.B: Engine.ToggleRun(); break;
        }
    }
    public void ReleaseInput(GameButton button)
    {
        if (button == GameButton.Left) Engine.Release(-1);
        if (button == GameButton.Right) Engine.Release(1);
    }
    public void Draw(ICanvas canvas, RectF bounds, GameDrawContext context) => screen.Draw(canvas, bounds, context);
}
