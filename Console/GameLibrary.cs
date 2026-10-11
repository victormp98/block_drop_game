using BlockDrop.Games.Tetris;
using BlockDrop.Games.Snake;
using BlockDrop.Games.Racing;
using BlockDrop.Games.Platformer;

namespace BlockDrop.ConsoleUI;

// This is the only place where the application selects concrete game modules.
public static class GameLibrary
{
    public static GameCatalog Create() => new([
        new TetrisModule(),
        new SnakeModule(),
        new RacingModule(),
        new PlatformerModule()
    ]);
}
