using BlockDrop.Shared.Contracts;

namespace BlockDrop.ConsoleUI;

public enum ScreenMode { Menu, Playing, Paused, Records, Help, GameOver }
public enum MenuAction { Play, Resume, NewGame, Records, Help }
public sealed record MenuEntry(string Label, MenuAction Action, string? GameId = null);

public sealed record ConsoleViewState(
    ScreenMode Mode,
    IRetroGame Game,
    int Best,
    IReadOnlyList<int> Records,
    IReadOnlyList<MenuEntry> MenuEntries,
    int Selection);
