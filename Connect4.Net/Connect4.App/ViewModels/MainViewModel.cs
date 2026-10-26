using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Connect4.App.Models;
using Connect4.App.Services;
using Connect4.Engine;

namespace Connect4.App.ViewModels;

/// <summary>
/// The game window: coordinates game modes (vs Computer Red/Yellow, vs Human), turn sequencing,
/// dual chess clocks, fair-play modified state tracking, and AI search background execution.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IEngineHost _engine;
    private readonly IDialogService _dialogs;
    private readonly IGameFileService _files;
    private readonly ISettingsStore _settingsStore;
    private readonly ISoundService _sounds;
    private readonly bool _settingsLoaded;

    // Both clocks before move at each ply, so taking back moves also gives the time back.
    private readonly Dictionary<int, (TimeSpan Red, TimeSpan Yellow)> _timeLeftAtPly = [];

    private Game _game = new();
    private Player _human = Player.Red;
    private string? _fileName;
    private string? _notice;
    private TimeSpan _redTimeLeft;
    private TimeSpan _yellowTimeLeft;
    private long _lastTickTimestamp;
    private readonly Timer _clockTimer;
    private readonly SynchronizationContext? _syncContext;
    private bool _hasHumanContinuedOnTime;
    private bool _isTimeoutPrompting;
    private bool _isGameModified;
    private bool _timedOut;
    private string? _timeoutStatus;

    private CancellationTokenSource? _cancel;
    private CancellationTokenSource? _moveNow;
    private int _searchId;

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _turnIndicatorText = "";

    [ObservableProperty]
    private string _moveCountText = "0 moves";

    [ObservableProperty]
    private string _sidesText = "";

    [ObservableProperty]
    private string _clockText = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private int _redPiecesCount;

    [ObservableProperty]
    private int _yellowPiecesCount;

    [ObservableProperty]
    private int _redKingsCount = 21;

    [ObservableProperty]
    private int _yellowKingsCount = 21;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    /// <summary>The colour a human would drop now; null while the computer thinks or when the game is over.</summary>
    [ObservableProperty]
    private Player? _nextDisc;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveNowCommand))]
    [NotifyCanExecuteChangedFor(nameof(SwitchSidesCommand))]
    private bool _isThinking;

    [ObservableProperty]
    private bool _isAnalysisVisible = true;

    [ObservableProperty]
    private bool _isSoundOn = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHumanVsComputer), nameof(IsComputerVsHuman), nameof(IsVsComputer), nameof(IsHumanVsHuman), nameof(RedPlayerLabel), nameof(YellowPlayerLabel), nameof(ShowAnalysis))]
    [NotifyCanExecuteChangedFor(nameof(SwitchSidesCommand))]
    private GameMode _mode;

    [ObservableProperty]
    private GameSettings _settings = GameSettings.Default;

    public bool IsGameModified
    {
        get => _isGameModified;
        set
        {
            if (SetProperty(ref _isGameModified, value))
            {
                OnPropertyChanged(nameof(IsStatusModified));
            }
        }
    }

    public bool IsStatusModified => IsVsComputer && IsGameModified;

    public bool IsHumanVsComputer => Mode == GameMode.HumanVsComputer;
    public bool IsComputerVsHuman => Mode == GameMode.ComputerVsHuman;
    public bool IsVsComputer => Mode is GameMode.HumanVsComputer or GameMode.ComputerVsHuman;
    public bool IsHumanVsHuman => Mode == GameMode.HumanVsHuman;
    public bool IsBoardFlipped => false;

    public bool ShowAnalysis => IsAnalysisVisible && IsVsComputer;
    public bool IsAiThinking => IsThinking;

    public string SettingsBadgeText => Settings.Summary;
    public string VariantBadgeText => Settings.VariantBadgeText;
    public bool IsGameSettingVisible => IsVsComputer;
    public bool IsClockVisible => Settings.Mode == TimeControlMode.TimePerGame;

    public TimeSpan RedTimeLeft => _redTimeLeft;
    public TimeSpan YellowTimeLeft => _yellowTimeLeft;
    public string RedClockText => FormatClock(_redTimeLeft);
    public string YellowClockText => FormatClock(_yellowTimeLeft);

    public TimeSpan ComputerTimeLeft => Mode switch
    {
        GameMode.ComputerVsHuman => _redTimeLeft,
        GameMode.HumanVsComputer => _yellowTimeLeft,
        _ => TimeSpan.Zero,
    };

    public string RedPlayerLabel => Mode switch
    {
        GameMode.HumanVsComputer => "Human",
        GameMode.ComputerVsHuman => "Computer",
        _ => "Player 1",
    };

    public string YellowPlayerLabel => Mode switch
    {
        GameMode.HumanVsComputer => "Computer",
        GameMode.ComputerVsHuman => "Human",
        _ => "Player 2",
    };

    public int RedDiscsCount => RedPiecesCount;
    public int YellowDiscsCount => YellowPiecesCount;
    public int RedDiscsLeft => RedKingsCount;
    public int YellowDiscsLeft => YellowKingsCount;

    public ObservableCollection<MoveHistoryItem> MoveHistoryList { get; } = [];

    public bool CanSwitchSides =>
        IsVsComputer &&
        IsHumanToMove &&
        !_game.IsGameOver &&
        !_timedOut &&
        !IsThinking;

    public MainViewModel(
        IEngineHost engine,
        IDialogService dialogs,
        IGameFileService files,
        ISettingsStore settingsStore,
        ISoundService sounds,
        string? startupNotice = null)
    {
        _engine = engine;
        _dialogs = dialogs;
        _files = files;
        _settingsStore = settingsStore;
        _sounds = sounds;

        AppSettings saved = settingsStore.Load().Normalize();
        Settings = saved.Game;
        Mode = saved.Mode;
        _human = Mode == GameMode.ComputerVsHuman ? Player.Yellow : Player.Red;
        IsAnalysisVisible = saved.ShowAnalysis;
        IsSoundOn = saved.SoundOn;
        WindowPlacement = saved.Window;
        _settingsLoaded = true;

        _redTimeLeft = Settings.GameTime;
        _yellowTimeLeft = Settings.GameTime;
        _notice = startupNotice;
        _syncContext = SynchronizationContext.Current;
        _clockTimer = new Timer(OnClockTimerTick, null, 250, 250);

        // Top row first, left to right, as a grid is filled.
        Cells = [.. Enumerable.Range(0, Position.Height).Reverse()
            .SelectMany(row => Enumerable.Range(0, Position.Width).Select(column => new CellViewModel(column, row)))];
        Start();
    }

    /// <summary>The 42 cells, top row first.</summary>
    public IReadOnlyList<CellViewModel> Cells { get; }

    public AnalysisViewModel Analysis { get; } = new();

    /// <summary>The saved window position, or null for the default position.</summary>
    public WindowPlacement? WindowPlacement { get; private set; }

    /// <summary>Completes when the computer has moved and a human is to move or the game is over.</summary>
    internal Task Idle { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Minimum time in milliseconds before the computer plays, so instant moves (book moves, forced moves) feel natural.
    /// Searches that take longer are not delayed. Tests can set it to 0.
    /// </summary>
    internal int ComputerDelayMs { get; set; } = 250;

    public bool IsRedActiveTurn => _game.ToMove == Player.Red && !_game.IsGameOver && !_timedOut;
    public bool IsYellowActiveTurn => _game.ToMove == Player.Yellow && !_game.IsGameOver && !_timedOut;

    public Game Game => _game;

    internal Player Human => _human;

    private bool IsHumanToMove => Mode == GameMode.HumanVsHuman || _game.ToMove == _human;

    /// <summary>Stops the computer without waiting, e.g. when the window closes.</summary>
    public void Stop() => _cancel?.Cancel();

    public void SaveWindowPlacement(WindowPlacement placement)
    {
        WindowPlacement = placement;
        SaveSettings();
    }

    partial void OnIsAnalysisVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowAnalysis));
        SaveSettings();
    }

    partial void OnIsSoundOnChanged(bool value) => SaveSettings();

    partial void OnModeChanged(GameMode value)
    {
        OnPropertyChanged(nameof(IsHumanVsComputer));
        OnPropertyChanged(nameof(IsComputerVsHuman));
        OnPropertyChanged(nameof(IsVsComputer));
        OnPropertyChanged(nameof(IsHumanVsHuman));
        OnPropertyChanged(nameof(RedPlayerLabel));
        OnPropertyChanged(nameof(YellowPlayerLabel));
        OnPropertyChanged(nameof(ShowAnalysis));
        OnPropertyChanged(nameof(IsGameSettingVisible));
        OnPropertyChanged(nameof(IsStatusModified));
        SwitchSidesCommand.NotifyCanExecuteChanged();
    }

    partial void OnSettingsChanged(GameSettings value)
    {
        OnPropertyChanged(nameof(SettingsBadgeText));
        OnPropertyChanged(nameof(VariantBadgeText));
        OnPropertyChanged(nameof(IsClockVisible));
        OnPropertyChanged(nameof(RedClockText));
        OnPropertyChanged(nameof(YellowClockText));
        OnPropertyChanged(nameof(ClockText));
    }

    private void SaveSettings()
    {
        if (_settingsLoaded && !_settingsStore.Save(new AppSettings(Settings, Mode, IsAnalysisVisible, IsSoundOn, WindowPlacement)))
        {
            Status = $"The settings could not be saved. {Status}";
            StatusText = Status;
        }
    }

    /// <summary>Drops a disc in the 0-based column for the human to move.</summary>
    [RelayCommand]
    private void Play(int column)
    {
        if (IsThinking || !IsHumanToMove || _timedOut || column is < 0 or >= Position.Width || !_game.CanPlay(column))
        {
            _dialogs.Beep();
            return;
        }

        RecordClockAtPly();
        _game.Play(column);
        PlayMoveSounds();
        Start();
    }

    public async Task StartNewGameWithMode(GameMode mode)
    {
        if (_game.Ply > 0 && !_game.IsGameOver && !_timedOut)
        {
            bool proceed = await _dialogs.ConfirmAsync("New Game", "Abandon current match and start a new game?");
            if (!proceed)
            {
                return;
            }
        }

        await StopAsync();
        Mode = mode;
        _human = mode switch
        {
            GameMode.HumanVsComputer => Player.Red,
            GameMode.ComputerVsHuman => Player.Yellow,
            _ => Player.Red,
        };
        _game.NewGame();
        _fileName = null;
        _timedOut = false;
        _timeoutStatus = null;
        IsGameModified = false;
        ResetClock();
        _engine.NewGame();
        Analysis.Clear();
        SaveSettings();
        Start();
    }

    [RelayCommand]
    private async Task SetMode(GameMode mode)
    {
        if (mode == Mode)
        {
            return;
        }

        await StopAsync();
        Mode = mode;

        // The human keeps the side to move; the computer takes the other colour.
        _human = _game.ToMove;
        if (Mode == GameMode.HumanVsComputer && _human == Player.Yellow)
        {
            Mode = GameMode.ComputerVsHuman;
        }

        SaveSettings();
        Analysis.Clear();
        Start();
    }

    [RelayCommand]
    public async Task NewGame()
    {
        if (_game.Ply > 0 && !_game.IsGameOver && !_timedOut)
        {
            bool proceed = await _dialogs.ConfirmAsync("New Game", "Abandon current match and start a new game?");
            if (!proceed)
            {
                return;
            }
        }

        await StopAsync();
        _game.NewGame();
        _fileName = null;
        _timedOut = false;
        _timeoutStatus = null;
        IsGameModified = false;
        _human = Mode == GameMode.ComputerVsHuman ? Player.Yellow : Player.Red;
        ResetClock();
        _engine.NewGame();
        Analysis.Clear();
        Start();
    }

    [RelayCommand]
    public async Task Open()
    {
        GameFile? file;
        Game loaded;
        try
        {
            file = await _files.OpenAsync();
            if (file is null)
            {
                return;
            }

            loaded = GameRecordFormat.Parse(file.Text);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
        {
            _dialogs.ShowError($"The game could not be opened.\n\n{exception.Message}");
            return;
        }

        await StopAsync();

        _game = loaded;
        _fileName = file.Name;
        _human = loaded.ToMove;
        if (Mode == GameMode.ComputerVsHuman && _human == Player.Red)
        {
            Mode = GameMode.HumanVsComputer;
        }
        else if (Mode == GameMode.HumanVsComputer && _human == Player.Yellow)
        {
            Mode = GameMode.ComputerVsHuman;
        }

        _timedOut = false;
        _timeoutStatus = null;
        IsGameModified = false;
        ResetClock();
        Analysis.Clear();
        Start();
    }

    [RelayCommand]
    public Task Save() => WriteAsync(askForName: false);

    [RelayCommand]
    public Task SaveAs() => WriteAsync(askForName: true);

    /// <summary>The human swaps sides with the computer mid-match.</summary>
    [RelayCommand(CanExecute = nameof(CanSwitchSides))]
    public async Task SwitchSides()
    {
        if (!CanSwitchSides)
        {
            return;
        }

        await StopAsync();
        _human = _human.Opponent();
        Mode = Mode == GameMode.HumanVsComputer ? GameMode.ComputerVsHuman : GameMode.HumanVsComputer;
        IsGameModified = true;

        if (IsHumanToMove && (_human == Player.Red ? _redTimeLeft : _yellowTimeLeft) <= TimeSpan.Zero)
        {
            _hasHumanContinuedOnTime = true;
        }
        else
        {
            _hasHumanContinuedOnTime = false;
        }

        SaveSettings();
        Start();
    }

    [RelayCommand(CanExecute = nameof(IsThinking))]
    public void MoveNow() => _moveNow?.Cancel();

    /// <summary>Against the computer: back to previous human move (2 plies). In 2-player: 1 ply.</summary>
    [RelayCommand(CanExecute = nameof(CanUndoMove))]
    public async Task Undo()
    {
        if (!CanUndoMove())
        {
            return;
        }

        await StopAsync();
        _timedOut = false;
        _timeoutStatus = null;

        if (IsVsComputer)
        {
            IsGameModified = true;
            do
            {
                _game.Undo();
            }
            while (_game.CanUndo && !IsHumanToMove);
        }
        else
        {
            _game.Undo();
        }

        RestoreClock();
        Start();
    }

    /// <summary>Against the computer: forward to next human move (2 plies). In 2-player: 1 ply.</summary>
    [RelayCommand(CanExecute = nameof(CanRedoMove))]
    public async Task Redo()
    {
        if (!CanRedoMove())
        {
            return;
        }

        await StopAsync();
        _timedOut = false;
        _timeoutStatus = null;

        if (IsVsComputer)
        {
            do
            {
                _game.Redo();
            }
            while (_game.CanRedo && !IsHumanToMove);
        }
        else
        {
            _game.Redo();
        }

        RestoreClock();
        Start();
    }

    [RelayCommand]
    public async Task EditSettings()
    {
        GameSettings? settings = await _dialogs.EditSettingsAsync(Settings);
        if (settings is null)
        {
            return;
        }

        bool clockChanged = settings.Mode != Settings.Mode || settings.MinutesPerGame != Settings.MinutesPerGame;
        bool limitsChanged = settings.Depth != Settings.Depth || settings.SecondsPerMove != Settings.SecondsPerMove || clockChanged;

        if (_game.Ply > 0 && IsVsComputer && limitsChanged)
        {
            IsGameModified = true;
        }

        Settings = settings.Normalize();
        SaveSettings();
        if (clockChanged)
        {
            ResetClock();
        }

        Refresh();
    }

    [RelayCommand]
    public void ToggleAnalysis() => IsAnalysisVisible = !IsAnalysisVisible;

    [RelayCommand]
    public void ToggleSound() => IsSoundOn = !IsSoundOn;

    [RelayCommand]
    public void About() => _dialogs.ShowAbout();

    private bool CanUndoMove() => _game.CanUndo && !IsThinking && !_timedOut;

    private bool CanRedoMove() => _game.CanRedo && !IsThinking && !_timedOut;

    private void Start() => Idle = RunAsync();

    private async Task StopAsync()
    {
        _cancel?.Cancel();
        await Idle;
    }

    // Plays computer moves until a human is to move or the game is over.
    private async Task RunAsync()
    {
        try
        {
            while (true)
            {
                Refresh();
                if (_timedOut)
                {
                    Status = WithNotice(_timeoutStatus ?? "Game over on time.");
                    StatusText = Status;
                    return;
                }

                if (_game.IsGameOver)
                {
                    Status = WithNotice(GameOverText());
                    StatusText = Status;
                    return;
                }

                if (IsHumanToMove)
                {
                    Status = WithNotice(Mode == GameMode.HumanVsHuman ? $"{_game.ToMove} to move." : "Your move.");
                    StatusText = Status;
                    return;
                }

                Status = WithNotice("Thinking…");
                StatusText = Status;
                if (!await ComputerMoveAsync())
                {
                    Refresh();
                    Status = "Stopped.";
                    StatusText = Status;
                    return;
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Status = "Error.";
            StatusText = Status;
            _dialogs.ShowError($"Something went wrong.\n\n{exception.Message}");
        }
    }

    private async Task<bool> ComputerMoveAsync()
    {
        using var cancel = new CancellationTokenSource();
        using var moveNow = new CancellationTokenSource();
        _cancel = cancel;
        _moveNow = moveNow;
        IsThinking = true;
        Analysis.Clear();

        Position position = _game.Position;
        RecordClockAtPly();
        SearchLimits limits = Settings.ToLimits(ComputerTimeLeft);

        int searchId = ++_searchId;
        var progress = new Progress<SearchInfo>(info =>
        {
            if (searchId == _searchId && IsThinking)
            {
                Analysis.Update(info, position);
            }
        });

        var stopwatch = Stopwatch.StartNew();
        try
        {
            SearchResult result = await _engine.ChooseMoveAsync(
                [.. _game.PlayedMoves], limits, progress, cancel.Token, moveNow.Token);

            Analysis.Update(result, position, stopwatch.Elapsed);
            await WaitForNaturalDelayAsync(stopwatch.Elapsed, cancel.Token, moveNow.Token);
            _game.Play(result.Column);
            PlayMoveSounds();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _cancel = null;
            _moveNow = null;
            IsThinking = false;
        }
    }

    // Instant moves (book moves, forced moves) wait until ComputerDelayMs has passed since the search started.
    // Moves that took longer are played at once, and Move Now skips the wait.
    private async Task WaitForNaturalDelayAsync(TimeSpan searchTime, CancellationToken cancel, CancellationToken moveNow)
    {
        int remainingMs = ComputerDelayMs - (int)searchTime.TotalMilliseconds;
        if (remainingMs <= 0 || moveNow.IsCancellationRequested)
        {
            return;
        }

        using var delay = CancellationTokenSource.CreateLinkedTokenSource(cancel, moveNow);
        try
        {
            await Task.Delay(remainingMs, delay.Token);
        }
        catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
        {
            // Move Now: play the move at once
        }
    }

    private void PlayMoveSounds()
    {
        if (!IsSoundOn)
        {
            return;
        }

        _sounds.Play(Sound.Drop);
        if (_game.IsGameOver)
        {
            _sounds.Play(_game.Winner switch
            {
                null => Sound.Draw,
                Player winner when Mode == GameMode.HumanVsComputer && winner != _human => Sound.Loss,
                Player winner when Mode == GameMode.ComputerVsHuman && winner != _human => Sound.Loss,
                _ => Sound.Win,
            });
        }
    }

    private void Refresh()
    {
        Position position = _game.Position;
        int? lastColumn = _game.LastMove;
        int lastRow = lastColumn is { } column ? TopRow(position, column) : -1;
        ulong winning = _game.WinningLine;

        int redCount = 0;
        int yellowCount = 0;

        foreach (CellViewModel cell in Cells)
        {
            Player? disc = position[cell.Column, cell.Row];
            cell.Disc = disc;
            cell.IsLastMove = cell.Column == lastColumn && cell.Row == lastRow;
            cell.IsWinning = (winning & Position.CellBit(cell.Column, cell.Row)) != 0;

            if (disc == Player.Red) redCount++;
            else if (disc == Player.Yellow) yellowCount++;
        }

        RedPiecesCount = redCount;
        YellowPiecesCount = yellowCount;
        RedKingsCount = Math.Max(0, 21 - redCount);
        YellowKingsCount = Math.Max(0, 21 - yellowCount);

        // Move History
        UpdateMoveHistory();

        if (Mode == GameMode.HumanVsHuman)
        {
            Analysis.Clear();
        }

        NextDisc = !_game.IsGameOver && !_timedOut && IsHumanToMove ? _game.ToMove : null;
        SidesText = Mode == GameMode.HumanVsComputer
            ? $"You play Red"
            : Mode == GameMode.ComputerVsHuman
                ? $"You play Yellow"
                : "Human vs human";

        TurnIndicatorText = _timedOut
            ? _timeoutStatus ?? "Game over on time."
            : _game.IsGameOver
                ? GameOverText()
                : $"{_game.ToMove}'s Turn — Drop a disc";

        TimeSpan compLeft = ComputerTimeLeft > TimeSpan.Zero ? ComputerTimeLeft : TimeSpan.Zero;
        ClockText = IsVsComputer && Settings.Mode == TimeControlMode.TimePerGame
            ? $"Computer time left: {(int)compLeft.TotalMinutes}:{compLeft.Seconds:00}"
            : "";

        Title = $"Connect 4 – {(_fileName is null ? "Untitled" : Path.GetFileName(_fileName))}";
        CanUndo = CanUndoMove();
        CanRedo = CanRedoMove();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        SwitchSidesCommand.NotifyCanExecuteChanged();
        UpdateClockProperties();
    }

    private void UpdateMoveHistory()
    {
        MoveHistoryList.Clear();
        IReadOnlyList<int> moves = [.. _game.PlayedMoves];
        for (int i = 0; i < moves.Count; i += 2)
        {
            int round = (i / 2) + 1;
            string red = (moves[i] + 1).ToString();
            string yellow = i + 1 < moves.Count ? (moves[i + 1] + 1).ToString() : "";
            string notation = string.IsNullOrEmpty(yellow) ? red : $"{red}    {yellow}";
            MoveHistoryList.Add(new MoveHistoryItem(round, notation, red, yellow));
        }

        // Counts individual moves (both players), the same in all four games.
        MoveCountText = moves.Count == 1 ? "1 move" : $"{moves.Count} moves";
    }

    private void OnClockTimerTick(object? state)
    {
        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => TickClock(), null);
        }
        else
        {
            TickClock();
        }
    }

    private void TickClock()
    {
        if (_isTimeoutPrompting ||
            Settings.Mode != TimeControlMode.TimePerGame ||
            _game.IsGameOver ||
            _timedOut ||
            _game.Ply == 0)
        {
            _lastTickTimestamp = 0;
            return;
        }

        long now = Stopwatch.GetTimestamp();
        if (_lastTickTimestamp == 0)
        {
            _lastTickTimestamp = now;
            return;
        }

        double elapsedSeconds = (now - _lastTickTimestamp) / (double)Stopwatch.Frequency;
        _lastTickTimestamp = now;

        if (elapsedSeconds <= 0)
        {
            return;
        }

        TimeSpan elapsed = TimeSpan.FromSeconds(elapsedSeconds);
        Player active = _game.ToMove;

        if (active == Player.Red)
        {
            if (IsHumanPlayer(Player.Red) && _hasHumanContinuedOnTime)
            {
                _redTimeLeft = TimeSpan.Zero;
            }
            else
            {
                _redTimeLeft = _redTimeLeft > elapsed ? _redTimeLeft - elapsed : TimeSpan.Zero;
                if (_redTimeLeft <= TimeSpan.Zero)
                {
                    _redTimeLeft = TimeSpan.Zero;
                    _ = HandleTimeoutAsync(Player.Red);
                    return;
                }
            }
        }
        else
        {
            if (IsHumanPlayer(Player.Yellow) && _hasHumanContinuedOnTime)
            {
                _yellowTimeLeft = TimeSpan.Zero;
            }
            else
            {
                _yellowTimeLeft = _yellowTimeLeft > elapsed ? _yellowTimeLeft - elapsed : TimeSpan.Zero;
                if (_yellowTimeLeft <= TimeSpan.Zero)
                {
                    _yellowTimeLeft = TimeSpan.Zero;
                    _ = HandleTimeoutAsync(Player.Yellow);
                    return;
                }
            }
        }

        UpdateClockProperties();
    }

    private bool IsHumanPlayer(Player player) =>
        Mode == GameMode.HumanVsHuman || (IsVsComputer && _human == player);

    private async Task HandleTimeoutAsync(Player timedOutPlayer)
    {
        if (_game.IsGameOver || _timedOut)
        {
            return;
        }

        _lastTickTimestamp = 0;

        if (IsHumanPlayer(timedOutPlayer) && !_hasHumanContinuedOnTime && IsVsComputer)
        {
            _isTimeoutPrompting = true;
            bool continuePlaying = false;
            try
            {
                continuePlaying = await _dialogs.ConfirmAsync(
                    "Time Expired",
                    "You ran out of time!\n\nDo you want to continue playing?");
            }
            finally
            {
                _isTimeoutPrompting = false;
            }

            if (continuePlaying)
            {
                _hasHumanContinuedOnTime = true;
                IsGameModified = true;
                Refresh();
                return;
            }

            await StopAsync();
            _timedOut = true;
            _timeoutStatus = "Game over, you lost on time.";
            Refresh();
            return;
        }

        if (!IsHumanPlayer(timedOutPlayer) && IsVsComputer)
        {
            await StopAsync();
            _timedOut = true;
            _timeoutStatus = "Game over, you won on time.";
            Refresh();
            return;
        }

        await StopAsync();
        _timedOut = true;
        _timeoutStatus = $"Game over, {timedOutPlayer.Opponent()} won on time.";
        Refresh();
    }

    private void UpdateClockProperties()
    {
        OnPropertyChanged(nameof(RedTimeLeft));
        OnPropertyChanged(nameof(YellowTimeLeft));
        OnPropertyChanged(nameof(RedClockText));
        OnPropertyChanged(nameof(YellowClockText));
        OnPropertyChanged(nameof(ClockText));
    }

    private static string FormatClock(TimeSpan span) =>
        span <= TimeSpan.Zero ? "0:00" : $"{(int)span.TotalMinutes}:{span.Seconds:00}";

    private void RecordClockAtPly()
    {
        _timeLeftAtPly[_game.Ply] = (_redTimeLeft, _yellowTimeLeft);
    }

    private void ResetClock()
    {
        _redTimeLeft = Settings.GameTime;
        _yellowTimeLeft = Settings.GameTime;
        _timeLeftAtPly.Clear();
        _lastTickTimestamp = 0;
        _hasHumanContinuedOnTime = false;
        _isTimeoutPrompting = false;
        UpdateClockProperties();
    }

    private void RestoreClock()
    {
        int next = _timeLeftAtPly.Keys.Where(ply => ply >= _game.Ply).DefaultIfEmpty(-1).Min();
        if (next >= 0 && _timeLeftAtPly.TryGetValue(next, out var times))
        {
            _redTimeLeft = times.Red;
            _yellowTimeLeft = times.Yellow;
        }
        else
        {
            _redTimeLeft = Settings.GameTime;
            _yellowTimeLeft = Settings.GameTime;
        }
        UpdateClockProperties();
    }

    private static int TopRow(Position position, int column)
    {
        for (int row = Position.Height - 1; row >= 0; row--)
        {
            if (position[column, row] is not null)
            {
                return row;
            }
        }

        return -1;
    }

    private string GameOverText() => (_game.Winner, Mode) switch
    {
        (null, _) => "Game over, a draw.",
        ({ } winner, GameMode.HumanVsHuman) => $"Game over, {winner} wins.",
        ({ } winner, _) when winner == _human => "Game over, you win.",
        _ => "Game over, the computer wins.",
    };

    private string WithNotice(string status)
    {
        string text = _notice is null ? status : $"{_notice} {status}";
        _notice = null;
        return text;
    }

    private async Task WriteAsync(bool askForName)
    {
        try
        {
            string? name = await _files.SaveAsync(GameRecordFormat.Format(_game) + Environment.NewLine, _fileName, askForName);
            if (name is not null)
            {
                _fileName = name;
                Refresh();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError($"The game could not be saved.\n\n{exception.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _clockTimer.Dispose();
    }
}
