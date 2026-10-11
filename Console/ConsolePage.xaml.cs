using System.Diagnostics;
using BlockDrop.Shared.Contracts;
using BlockDrop.Shared.Storage;
using Microsoft.Maui.Dispatching;

namespace BlockDrop.ConsoleUI;

public partial class ConsolePage : ContentPage
{
    private readonly ConsoleController controller;
    private readonly ConsoleScreen screen;
    private readonly Stopwatch clock = new();
    private readonly IDispatcherTimer timer;
    private double lastTick;
    private GameButton? heldDirection;
    private double repeatAt;
    private Window? subscribedWindow;
    private string? displayedGameId;
#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? keyboardRoot;
    private readonly HashSet<Windows.System.VirtualKey> pressedKeys = [];
#endif

    public ConsolePage()
    {
        InitializeComponent();
        var scores = new GameScoreRepository(GetDataDirectory());
        controller = new ConsoleController(GameLibrary.Create(), scores.Open);
        screen = new ConsoleScreen(() => controller.View);
        ScreenView.Drawable = screen;
        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(33);
        timer.Tick += OnTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Refresh();
    }

    private static string GetDataDirectory()
    {
#if WINDOWS
        return Environment.GetEnvironmentVariable("BLOCKDROP_DATA_DIRECTORY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlockDrop");
#else
        return FileSystem.AppDataDirectory;
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RestartTimer();
    }

    protected override void OnDisappearing()
    {
        SuspendConsole();
        timer.Stop();
        base.OnDisappearing();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Window is { } window && subscribedWindow != window)
        {
            UnsubscribeWindow();
            subscribedWindow = window;
            window.Deactivated += OnWindowDeactivated;
            window.Stopped += OnWindowDeactivated;
            window.Activated += OnWindowActivated;
            window.Destroying += OnWindowDestroying;
        }
#if WINDOWS
        if (keyboardRoot is null && Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window native)
        {
            keyboardRoot = native.Content;
            keyboardRoot.AddHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyDownEvent,
                new Microsoft.UI.Xaml.Input.KeyEventHandler(OnKeyDown), true);
            keyboardRoot.AddHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyUpEvent,
                new Microsoft.UI.Xaml.Input.KeyEventHandler(OnKeyUp), true);
        }
#endif
        Refresh();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        controller.Close();
        heldDirection = null;
        timer.Stop();
        UnsubscribeWindow();
#if WINDOWS
        if (keyboardRoot is not null)
        {
            keyboardRoot.RemoveHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyDownEvent,
                new Microsoft.UI.Xaml.Input.KeyEventHandler(OnKeyDown));
            keyboardRoot.RemoveHandler(Microsoft.UI.Xaml.UIElement.PreviewKeyUpEvent,
                new Microsoft.UI.Xaml.Input.KeyEventHandler(OnKeyUp));
            keyboardRoot = null;
        }
#endif
    }

    private void UnsubscribeWindow()
    {
        if (subscribedWindow is null) return;
        subscribedWindow.Deactivated -= OnWindowDeactivated;
        subscribedWindow.Stopped -= OnWindowDeactivated;
        subscribedWindow.Activated -= OnWindowActivated;
        subscribedWindow.Destroying -= OnWindowDestroying;
        subscribedWindow = null;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        SuspendConsole();
#if WINDOWS
        pressedKeys.Clear();
#endif
        timer.Stop();
    }

    private void OnWindowActivated(object? sender, EventArgs e) => RestartTimer();

    private void RestartTimer()
    {
        clock.Restart();
        lastTick = 0;
        timer.Start();
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        controller.Close();
        timer.Stop();
    }

    private void SuspendConsole()
    {
        heldDirection = null;
        controller.Suspend();
        Refresh();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = clock.Elapsed.TotalSeconds;
        var elapsed = Math.Min(0.25, now - lastTick);
        lastTick = now;
        if (heldDirection is { } direction && now >= repeatAt)
        {
            Send(direction);
            repeatAt = now + (direction == GameButton.Down ? 0.055 : 0.075);
        }
        controller.Update(elapsed);
        var blink = ((int)(now * 2) & 1) == 1;
        if (controller.Mode is ScreenMode.Playing or ScreenMode.GameOver || screen.Blink != blink)
        {
            screen.Blink = blink;
            Refresh();
        }
    }

    private void Refresh()
    {
        if (controller.Mode is not (ScreenMode.Playing or ScreenMode.Menu)) heldDirection = null;
        var definition = controller.CurrentGame.Definition;
        if (displayedGameId != definition.Id)
        {
            displayedGameId = definition.Id;
            ActionALabel.Text = definition.ActionA;
            ActionBLabel.Text = definition.ActionB;
            ScreenCaptionLabel.Text = definition.ScreenCaption;
            SemanticProperties.SetDescription(ActionAButton, $"A: {definition.ActionA.ToLowerInvariant()} o confirmar");
            SemanticProperties.SetDescription(ActionBButton, $"B: {definition.ActionB.ToLowerInvariant()} o volver");
        }
        ScreenView.Invalidate();
    }

    private void Send(GameButton button)
    {
        var previousMode = controller.Mode;
        var previousGame = controller.CurrentGame;
        controller.HandleButton(button);
        if (previousMode != controller.Mode || previousGame != controller.CurrentGame) heldDirection = null;
        Refresh();
    }

    private static GameButton? DirectionFor(object? sender) => sender is Button { CommandParameter: string direction }
        ? direction switch { "left" => GameButton.Left, "right" => GameButton.Right, "down" => GameButton.Down, _ => null }
        : null;

    private void OnDirectionPressed(object? sender, EventArgs e)
    {
        if (DirectionFor(sender) is { } direction) BeginHold(direction);
    }

    private void BeginHold(GameButton direction)
    {
        if (heldDirection is { } previous && previous != direction) controller.ReleaseButton(previous);
        Send(direction);
        if (controller.Mode is ScreenMode.Playing or ScreenMode.Menu)
        {
            heldDirection = direction;
            repeatAt = clock.Elapsed.TotalSeconds + 0.24;
        }
    }

    private void OnDirectionReleased(object? sender, EventArgs e)
    {
        if (DirectionFor(sender) is { } direction) controller.ReleaseButton(direction);
        if (DirectionFor(sender) == heldDirection) heldDirection = null;
    }

    private void OnUpClicked(object? sender, EventArgs e) => Send(GameButton.Up);
    private void OnAClicked(object? sender, EventArgs e) => Send(GameButton.A);
    private void OnBClicked(object? sender, EventArgs e) => Send(GameButton.B);
    private void OnSelectClicked(object? sender, EventArgs e) => Send(GameButton.Select);
    private void OnStartClicked(object? sender, EventArgs e) => Send(GameButton.Start);

    private void OnScreenTapped(object? sender, TouchEventArgs e)
    {
        if (controller.Mode == ScreenMode.Menu && e.Touches.Length > 0)
        {
            controller.SelectMenuItem(screen.MenuItemAt(e.Touches[0]));
            heldDirection = null;
            Refresh();
        }
        else if (controller.Mode is ScreenMode.Help or ScreenMode.Records) Send(GameButton.Select);
    }

    protected override bool OnBackButtonPressed()
    {
        if (!controller.Back()) return base.OnBackButtonPressed();
        heldDirection = null;
        Refresh();
        return true;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (ConsoleLayout is null || width <= 0) return;
        var compact = height < 640;
        ConsoleLayout.Padding = compact ? new Thickness(12, 8, 12, 6) : new Thickness(16, 14, 16, 10);
        ConsoleLayout.RowSpacing = compact ? 6 : 10;
        ControlsPanel.RowSpacing = compact ? 2 : 8;
        Speaker.IsVisible = width >= 360;
        ScreenView.Invalidate();
    }

#if WINDOWS
    private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var button = e.Key switch
        {
            Windows.System.VirtualKey.Left => GameButton.Left,
            Windows.System.VirtualKey.Right => GameButton.Right,
            Windows.System.VirtualKey.Down => GameButton.Down,
            Windows.System.VirtualKey.Up => GameButton.Up,
            Windows.System.VirtualKey.A => GameButton.A,
            Windows.System.VirtualKey.Space or Windows.System.VirtualKey.B => GameButton.B,
            Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.P => GameButton.Start,
            Windows.System.VirtualKey.Escape => GameButton.Select,
            _ => (GameButton?)null
        };
        if (button is null) return;
        e.Handled = true;
        if (!pressedKeys.Add(e.Key)) return;
        if (button is GameButton.Left or GameButton.Right or GameButton.Down) BeginHold(button.Value);
        else Send(button.Value);
    }

    private void OnKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        pressedKeys.Remove(e.Key);
        var direction = e.Key switch
        {
            Windows.System.VirtualKey.Left => GameButton.Left,
            Windows.System.VirtualKey.Right => GameButton.Right,
            Windows.System.VirtualKey.Down => GameButton.Down,
            _ => (GameButton?)null
        };
        if (direction is { } released) controller.ReleaseButton(released);
        if (direction is not null && heldDirection == direction) heldDirection = null;
    }
#endif
}
