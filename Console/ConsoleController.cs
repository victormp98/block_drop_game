using BlockDrop.Shared.Contracts;
using BlockDrop.Shared.Storage;

namespace BlockDrop.ConsoleUI;

// Routes input and lifecycle events without knowing any game's rules.
public sealed class ConsoleController
{
    private sealed class Session(IRetroGame game, ScoreStore scores)
    {
        public IRetroGame Game { get; } = game;
        public ScoreStore Scores { get; } = scores;
        public bool RoundSaved { get; set; }
        public void SaveRound()
        {
            Scores.RememberBest(Game.Score);
            if (Game.Started && !RoundSaved && Game.Score > 0)
            {
                Scores.AddRound(Game.Score);
                RoundSaved = true;
            }
            Scores.Flush();
        }
    }

    private readonly GameCatalog catalog;
    private readonly Dictionary<string, Session> sessions;
    private Session current;
    public ScreenMode Mode { get; private set; } = ScreenMode.Menu;
    public int Selection { get; private set; }
    public IReadOnlyList<MenuEntry> MenuEntries { get; private set; } = [];
    public IRetroGame CurrentGame => current.Game;
    public ConsoleViewState View => new(Mode, current.Game, current.Scores.Best, current.Scores.Records, MenuEntries, Selection);

    public ConsoleController(GameCatalog catalog, Func<GameDefinition, ScoreStore> createScores)
    {
        this.catalog = catalog;
        sessions = catalog.Games.ToDictionary(game => game.Definition.Id,
            game => new Session(game, createScores(game.Definition)), StringComparer.Ordinal);
        current = sessions[catalog.Games[0].Definition.Id];
        ShowMenu();
    }

    public void HandleButton(GameButton button)
    {
        if (button == GameButton.Select) { ShowMenu(); return; }
        switch (Mode)
        {
            case ScreenMode.Menu:
                if (button is GameButton.Up or GameButton.Left) MoveSelection(-1);
                else if (button is GameButton.Down or GameButton.Right) MoveSelection(1);
                else if (button is GameButton.A or GameButton.Start) Confirm();
                else if (button == GameButton.B && CanResume) Resume();
                break;
            case ScreenMode.Playing:
                if (button == GameButton.Start) Suspend();
                else { current.Game.HandleInput(button); RefreshGame(); }
                break;
            case ScreenMode.Paused:
                if (button is GameButton.Start or GameButton.A) Resume();
                else if (button == GameButton.B) ShowMenu();
                break;
            case ScreenMode.GameOver:
                if (button is GameButton.Start or GameButton.A) StartNewGame();
                else if (button == GameButton.B) ShowMenu();
                break;
            case ScreenMode.Records:
            case ScreenMode.Help:
                if (button is GameButton.A or GameButton.B or GameButton.Start) ShowMenu();
                break;
        }
    }

    public void SelectMenuItem(int index)
    {
        if (Mode != ScreenMode.Menu || index < 0 || index >= MenuEntries.Count) return;
        Selection = index;
        Confirm();
    }

    public void ReleaseButton(GameButton button) => current.Game.ReleaseInput(button);

    public void Update(double seconds)
    {
        if (Mode != ScreenMode.Playing) return;
        current.Game.Update(seconds);
        RefreshGame();
    }

    public void Suspend()
    {
        current.Game.SetPaused(true);
        if (Mode == ScreenMode.Playing) Mode = ScreenMode.Paused;
        current.Scores.RememberBest(current.Game.Score);
        current.Scores.Flush();
    }

    public void ShowMenu()
    {
        Suspend();
        Mode = ScreenMode.Menu;
        var entries = new List<MenuEntry>();
        if (CanResume)
        {
            entries.Add(new("CONTINUAR", MenuAction.Resume));
            entries.Add(new("NUEVA PARTIDA", MenuAction.NewGame));
        }
        foreach (var game in catalog.Games)
        {
            if (CanResume && game == current.Game) continue;
            entries.Add(new(catalog.Games.Count == 1 ? "JUGAR" : game.Definition.Title, MenuAction.Play, game.Definition.Id));
        }
        entries.Add(new("RECORDS", MenuAction.Records));
        entries.Add(new("CONTROLES", MenuAction.Help));
        MenuEntries = entries.AsReadOnly();
        Selection = 0;
    }

    public void PlayGame(string id)
    {
        // Validate before pausing the current session.
        var game = catalog.Find(id);
        Suspend();
        current = sessions[game.Definition.Id];
        if (CanResume) Resume();
        else StartNewGame();
    }

    public bool Back()
    {
        if (Mode == ScreenMode.Menu) return false;
        if (Mode == ScreenMode.Playing) Suspend();
        else ShowMenu();
        return true;
    }

    public void Close()
    {
        Suspend();
        foreach (var session in sessions.Values) session.SaveRound();
    }

    private bool CanResume => current.Game.Started && !current.Game.GameOver;

    private void MoveSelection(int direction) => Selection = (Selection + direction + MenuEntries.Count) % MenuEntries.Count;

    private void Confirm()
    {
        var entry = MenuEntries[Selection];
        switch (entry.Action)
        {
            case MenuAction.Play: PlayGame(entry.GameId!); break;
            case MenuAction.Resume: Resume(); break;
            case MenuAction.NewGame: StartNewGame(); break;
            case MenuAction.Records: Mode = ScreenMode.Records; break;
            case MenuAction.Help: Mode = ScreenMode.Help; break;
        }
    }

    private void StartNewGame()
    {
        current.SaveRound();
        current.RoundSaved = false;
        current.Game.Start();
        Mode = ScreenMode.Playing;
        RefreshGame();
    }

    private void Resume()
    {
        current.Game.SetPaused(false);
        Mode = ScreenMode.Playing;
        RefreshGame();
    }

    private void RefreshGame()
    {
        current.Scores.RememberBest(current.Game.Score);
        if (!current.Game.GameOver) return;
        Mode = ScreenMode.GameOver;
        current.SaveRound();
    }
}
