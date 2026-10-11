using Microsoft.Maui.Graphics;

namespace BlockDrop.Shared.Contracts;

public enum GameButton { Up, Down, Left, Right, A, B, Start, Select }

public sealed record GameDefinition(
    string Id,
    string Title,
    string ScreenCaption,
    string ActionA,
    string ActionB,
    IReadOnlyList<string> HelpLines,
    string? LegacyScoreFile = null);

public readonly record struct GameDrawContext(int Best);

// Games own their rules and drawing. Menu, pause navigation and saving belong to the console.
public interface IRetroGame
{
    GameDefinition Definition { get; }
    bool Started { get; }
    bool Paused { get; }
    bool GameOver { get; }
    bool Won => false;
    int Score { get; }
    void Start();
    void SetPaused(bool paused);
    void Update(double seconds);
    void HandleInput(GameButton button);
    void ReleaseInput(GameButton button) { }
    // Bounds are logical LCD coordinates, already scaled by the console renderer.
    void Draw(ICanvas canvas, RectF bounds, GameDrawContext context);
}
