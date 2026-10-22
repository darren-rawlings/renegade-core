using System;
using System.Collections.Generic;
using Godot;
using Renegade.Core;
using Renegade.Looks;
using Renegade.Play;
using Renegade.Store;

namespace Renegade.Ui;

/// <summary>
/// What a game's HUD offers. <see cref="LevelOnly"/> replaces the two mode buttons with a single
/// "Level" button, for solo games; <see cref="UndoText"/> renames the rewarded-ad button (a hint, say).
/// </summary>
public sealed record HudOptions(
	string Title,
	string Hint,
	bool Modes = true,
	bool StyleMenu = true,
	string PiecesLabel = "Pieces",
	Func<Style, string>? PieceName = null,
	string NewGameText = "New Game",
	string UndoText = "Undo (ad)",
	bool LevelOnly = false,
	bool Restart = true,
	// Games with nothing to spin (card games) leave "Freeze the board" out of Settings.
	bool FreezeSetting = true,
	// A game's own Style menu (card backs and fronts, say) in place of the board and pieces one.
	Action? CustomStyle = null);

/// <summary>
/// The studio's game HUD, laid out for a 720×1280 portrait phone. Top: the title and score, with a
/// row of small buttons (Style and How to play on the left, Start over and Settings on the right).
/// Bottom: the fading controls hint, the turn status, New Game, Undo (ad), and the mode buttons with
/// a difficulty pop-up. Sound and Freeze board live in Settings, with each game's own options.
/// Everything that isn't a button ignores the mouse so taps reach the 3D board underneath.
/// </summary>
public partial class GameHud : CanvasLayer
{
	public event Action? NewGamePressed;
	public event Action? UndoPressed;

	/// <summary>The tutorial (Demo) opened or closed: a game can hold play while it's up.</summary>
	public event Action? TutorialOpened;
	public event Action? TutorialClosed;

	public bool TutorialOpen => _tutorial?.Visible == true;
	public event Action? RestartPressed;
	public event Action<bool>? MuteToggled;
	public event Action<bool>? FreezeToggled;
	public event Action<GameMode, Difficulty>? ModeChosen;
	public event Action<Style>? BoardChosen;
	public event Action<Style>? PiecesChosen;

	private Label _score = null!;
	private Label _status = null!;
	private Label _hint = null!;
	private Button _newGame = null!;
	private Button _undo = null!;
	private string _undoText = "";
	private Button? _restart;
	private Button? _pvp;
	private Button? _pvc;
	private Control? _difficultyPopup;
	private Control? _menu;
	private SettingsPanel _settings = null!;
	private TutorialPanel? _tutorial;
	private Button _howTo = null!;
	private string _title = "";
	private bool _levelOnly;
	private bool _modesLocked;
	private int _dialogs;

	public GameHud() : this(HudTheme.Warm) { }

	public GameHud(HudTheme theme)
	{
		Theme = theme;
	}

	public HudTheme Theme { get; }

	public void Build(HudOptions options, GameSettings settings)
	{
		_title = options.Title;
		var heading = Widgets.Label(options.Title, 54, Theme.Text);
		Widgets.Anchor(heading, Control.LayoutPreset.TopWide, top: 34, bottom: 104);
		AddChild(heading);

		_score = Widgets.Label("", 26, Theme.TextMuted);
		Widgets.Anchor(_score, Control.LayoutPreset.TopWide, top: 100, bottom: 140);
		AddChild(_score);

		_hint = Widgets.Label(options.Hint, 24, Theme.TextMuted);
		Widgets.Anchor(_hint, Control.LayoutPreset.BottomWide, top: -290, bottom: -254);
		AddChild(_hint);

		_status = Widgets.Label("", 36, Theme.Text);
		Widgets.Anchor(_status, Control.LayoutPreset.BottomWide, top: -250, bottom: -196);
		AddChild(_status);

		_newGame = Widgets.Button(options.NewGameText, 30, Theme.Accent, Theme);
		Widgets.Anchor(_newGame, Control.LayoutPreset.CenterBottom, left: -150, right: 150, top: -192, bottom: -128);
		_newGame.Pressed += () => NewGamePressed?.Invoke();
		_newGame.Visible = false;
		AddChild(_newGame);

		// The rewarded-ad button says so in its label.
		_undo = Widgets.Button(options.UndoText, 22, Theme.ButtonDark, Theme);
		Widgets.Anchor(_undo, Control.LayoutPreset.BottomLeft, left: 20, right: 180, top: -186, bottom: -130);
		_undo.Pressed += () => UndoPressed?.Invoke();
		_undo.Visible = false;
		AddChild(_undo);

		// The top row: small, so the board keeps the screen.
		if (options.StyleMenu)
		{
			var style = Widgets.Button("Style", 22, Theme.ButtonDark, Theme);
			Widgets.Anchor(style, Control.LayoutPreset.TopLeft, left: 20, right: 124, top: 148, bottom: 196);
			style.Pressed += () =>
			{
				if (options.CustomStyle != null)
					options.CustomStyle();
				else
					Widgets.FadeIn(_menu!);
			};
			AddChild(style);
		}
		// "Demo": the guided tour with video clips of the game.
		_howTo = Widgets.Button("Demo", 22, Theme.ButtonDark, Theme);
		Widgets.Anchor(_howTo, Control.LayoutPreset.TopLeft, left: options.StyleMenu ? 134 : 20, right: options.StyleMenu ? 234 : 120, top: 148, bottom: 196);
		_howTo.Pressed += () => _tutorial?.Offer();
		_howTo.Visible = false;
		AddChild(_howTo);

		var settingsButton = Widgets.Button("Settings", 22, Theme.ButtonDark, Theme);
		Widgets.Anchor(settingsButton, Control.LayoutPreset.TopRight, left: -144, right: -20, top: 148, bottom: 196);
		settingsButton.Pressed += () => _settings.Open();
		AddChild(settingsButton);
		if (options.Restart)
		{
			_restart = Widgets.Button("Start over", 22, Theme.ButtonDark, Theme);
			Widgets.Anchor(_restart, Control.LayoutPreset.TopRight, left: -290, right: -154, top: 148, bottom: 196);
			_restart.Pressed += () => RestartPressed?.Invoke();
			AddChild(_restart);
		}

		if (options.Modes || options.LevelOnly)
			BuildModeButtons(settings.Mode, settings.Difficulty, options.LevelOnly);

		if (options.StyleMenu && options.CustomStyle == null)
		{
			_menu = BuildMenu(settings.Board, settings.Pieces, options.PiecesLabel, options.PieceName ?? (s => Styles.Pieces(s).Name));
			_menu.Visible = false;
			AddChild(_menu);
		}

		_settings = new SettingsPanel(Theme);
		// The HUD keeps these two itself: games only read settings.Muted and settings.FreezeBoard.
		_settings.AddToggle("Sound", !settings.Muted, on =>
		{
			settings.Muted = !on;
			AudioServer.SetBusMute(0, !on);
			settings.Save();
			MuteToggled?.Invoke(!on);
		});
		if (options.FreezeSetting)
		{
			_settings.AddToggle("Freeze the board (no spinning)", settings.FreezeBoard, on =>
			{
				settings.FreezeBoard = on;
				settings.Save();
				FreezeToggled?.Invoke(on);
			});
		}
		AddChild(_settings);

		// The shop at the foot of Settings; once ads are removed, "(ad)" leaves the labels.
		_undoText = options.UndoText;
		ShopRows.Add(_settings, this, message => Dialog("", message, ("OK", true, null)));
		if (Shop.Current is { } shop)
		{
			void Relabel() => _undo.Text = ShopRows.AdText(_undoText);
			shop.Changed += Relabel;
			TreeExiting += () => shop.Changed -= Relabel;
			Relabel();
		}
	}

	/// <summary>True while a menu, pop-up or dialog is open; the board ignores taps meanwhile.</summary>
	public bool MenuOpen => _difficultyPopup?.Visible == true || _menu?.Visible == true || _settings.Visible || _tutorial?.Visible == true || _dialogs > 0;

	// ---------------------------------------------------------------- Game-specific additions

	/// <summary>Adds an On/Off row to Settings.</summary>
	public void AddToggle(string label, bool value, Action<bool> changed) => _settings.AddToggle(label, value, changed);

	/// <summary>Adds a row of options to Settings.</summary>
	public void AddChoice(string label, string[] options, int selected, Action<int> chosen) => _settings.AddChoice(label, options, selected, chosen);

	/// <summary>A button in the HUD's style, for a game's own controls (Roll, Check, Solve...).</summary>
	public Button AddButton(string text, bool accent, Control.LayoutPreset preset, float left, float right, float top, float bottom, int fontSize = 24)
	{
		var button = Widgets.Button(text, fontSize, accent ? Theme.Accent : Theme.ButtonDark, Theme);
		Widgets.Anchor(button, preset, left, right, top, bottom);
		AddChild(button);
		// Beneath the menus, Settings, the tutorial and dialogs (all added after Undo), never over them.
		MoveChild(button, _undo.GetIndex() + 1);
		return button;
	}

	/// <summary>Sets the tutorial behind the Demo button.</summary>
	public void SetTutorial(IReadOnlyList<TutorialPage> pages)
	{
		_tutorial?.QueueFree();
		_tutorial = new TutorialPanel(Theme, _title, pages);
		_tutorial.Opened += () => TutorialOpened?.Invoke();
		_tutorial.Closed += () => TutorialClosed?.Invoke();
		AddChild(_tutorial);
		_howTo.Visible = pages.Count > 0;
	}

	/// <summary>Developer checks: `--tutorial=N` opens the tour at step N (0 = the invitation), `--settings` opens Settings.</summary>
	public void ApplyDevFlags()
	{
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith("--tutorial=") && int.TryParse(arg[11..], out int page))
			{
				if (page == 0)
					_tutorial?.Offer();
				else
					_tutorial?.OpenAt(page - 1);
			}
			else if (arg == "--settings")
			{
				_settings.Open();
			}
		}
	}

	/// <summary>Offers the tutorial the first time the game is opened, once.</summary>
	public void OfferTutorialOnce(GameSettings settings)
	{
		if (_tutorial == null || settings.Extra.ContainsKey("tutorial_offered"))
			return;
		settings.Extra["tutorial_offered"] = "1";
		settings.Save();
		_tutorial.Offer();
	}

	/// <summary>
	/// Confirms starting over. Against the computer that's a resignation, a loss, unless the player
	/// watches an ad: <paramref name="restart"/> is called with true when the loss should count.
	/// </summary>
	public void AskRestart(bool vsComputer, Action<bool> restart, Action<Action>? watchAd = null)
	{
		if (vsComputer && Shop.Current?.AdsRemoved == true)
		{
			// No ads to watch: starting over is simply free.
			Dialog("Start over?", "This game will be cleared away and a new one started. It won't count as a loss.",
				("Start over", true, () => restart(false)),
				("Keep playing", false, null));
			return;
		}
		if (vsComputer)
		{
			Dialog("Start over?",
				"Against the computer, starting over counts as resigning: a loss. Watch a short ad and it won't count.",
				("Watch an ad: no loss", true, watchAd == null ? () => restart(false) : () => watchAd(() => restart(false))),
				("Resign and start over", false, () => restart(true)),
				("Keep playing", false, null));
		}
		else
		{
			Dialog("Start over?", "This game will be thrown away and a new one started.",
				("Start over", true, () => restart(false)),
				("Keep playing", false, null));
		}
	}

	/// <summary>Shows a dialog; the board ignores taps while it's open.</summary>
	public void Dialog(string title, string message, params (string Text, bool Accent, Action? Pressed)[] buttons)
	{
		_dialogs++;
		var wrapped = new (string Text, bool Accent, Action? Pressed)[buttons.Length];
		for (int i = 0; i < buttons.Length; i++)
		{
			var button = buttons[i];
			wrapped[i] = (button.Text, button.Accent, () =>
			{
				_dialogs--;
				button.Pressed?.Invoke();
			});
		}
		ConfirmDialog.Show(this, Theme, title, message, wrapped);
	}

	// ---------------------------------------------------------------- Updates

	public void SetStatus(string text, Color color)
	{
		_status.Text = text;
		_status.AddThemeColorOverride("font_color", color);
	}

	public void SetScore(string text) => _score.Text = text;

	public void SetNewGameVisible(bool visible) => _newGame.Visible = visible;

	public void SetUndoVisible(bool visible) => _undo.Visible = visible;

	/// <summary>
	/// Locks the mode and level buttons while a game is under way (they dim, and a tap explains),
	/// so the level can only change between games.
	/// </summary>
	public void SetModesLocked(bool locked)
	{
		if (_modesLocked == locked)
			return;
		_modesLocked = locked;
		foreach (var button in new[] { _pvp, _pvc })
			if (button != null)
				button.Modulate = new Color(1, 1, 1, locked ? 0.5f : 1f);
	}

	private bool LockedNotice()
	{
		if (_modesLocked)
			Dialog("", _levelOnly ? "The level can be changed between games." : "The mode and level can be changed between games.", ("OK", true, null));
		return _modesLocked;
	}

	public void SetRestartVisible(bool visible)
	{
		if (_restart != null)
			_restart.Visible = visible;
	}

	/// <summary>The controls hint fades after the first move; it's only needed once.</summary>
	public void HideHint()
	{
		if (_hint.Modulate.A < 1f)
			return;
		CreateTween().TweenProperty(_hint, "modulate:a", 0f, 0.6f);
	}

	// ---------------------------------------------------------------- Modes

	/// <summary>
	/// Two buttons side by side at the bottom. Choosing "vs Computer" pops up the difficulty choice
	/// first; the mode only changes once a level is picked, and tapping outside leaves it as it was.
	/// </summary>
	private void BuildModeButtons(GameMode mode, Difficulty difficulty, bool levelOnly)
	{
		_levelOnly = levelOnly;
		_pvp = Widgets.Button("2 Players", 24, Theme.ButtonDark, Theme);
		Widgets.Anchor(_pvp, Control.LayoutPreset.CenterBottom, left: -330, right: -10, top: -110, bottom: -46);
		_pvp.Visible = !levelOnly;
		AddChild(_pvp);

		_pvc = Widgets.Button("", 24, Theme.ButtonDark, Theme);
		if (levelOnly)
			Widgets.Anchor(_pvc, Control.LayoutPreset.CenterBottom, left: -170, right: 170, top: -110, bottom: -46);
		else
			Widgets.Anchor(_pvc, Control.LayoutPreset.CenterBottom, left: 10, right: 330, top: -110, bottom: -46);
		AddChild(_pvc);

		_pvp.Pressed += () =>
		{
			if (!LockedNotice())
				ChooseMode(GameMode.PlayerVsPlayer, difficulty);
		};
		_pvc.Pressed += () =>
		{
			if (!LockedNotice())
				Widgets.FadeIn(_difficultyPopup!);
		};

		// Transparent full-screen catcher: a tap anywhere outside the three buttons closes it.
		var catcher = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
		catcher.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		catcher.GuiInput += input =>
		{
			if (input is InputEventMouseButton { Pressed: true })
				catcher.Visible = false;
		};
		_difficultyPopup = catcher;

		var panel = Widgets.Panel(Theme, 14);
		Widgets.Anchor(panel, Control.LayoutPreset.CenterBottom, left: -230, right: 230, top: -200, bottom: -118);
		catcher.AddChild(panel);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		panel.AddChild(row);
		foreach (var level in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
		{
			var button = Widgets.Button(level.ToString(), 26, Theme.Accent, Theme);
			button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			button.Pressed += () =>
			{
				difficulty = level;
				catcher.Visible = false;
				ChooseMode(GameMode.PlayerVsComputer, level);
			};
			row.AddChild(button);
		}
		catcher.Visible = false;
		AddChild(catcher);

		ShowMode(mode, difficulty);
	}

	private void ChooseMode(GameMode mode, Difficulty difficulty)
	{
		ShowMode(mode, difficulty);
		ModeChosen?.Invoke(mode, difficulty);
	}

	private void ShowMode(GameMode mode, Difficulty difficulty)
	{
		bool vsComputer = mode == GameMode.PlayerVsComputer || _levelOnly;
		_pvc!.Text = _levelOnly ? $"Level: {difficulty}" : vsComputer ? $"vs Computer: {difficulty}" : "vs Computer";
		Widgets.Restyle(_pvp!, vsComputer ? Theme.ButtonDark : Theme.Accent, Theme);
		Widgets.Restyle(_pvc, vsComputer ? Theme.Accent : Theme.ButtonDark, Theme);
	}

	// ---------------------------------------------------------------- Style menu

	private Control BuildMenu(Style board, Style pieces, string piecesLabel, Func<Style, string> pieceName)
	{
		// A light scrim blocks taps on the board while still showing style changes live behind it.
		var scrim = new ColorRect { Color = new Color(0, 0, 0, 0.25f), MouseFilter = Control.MouseFilterEnum.Stop };
		scrim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		var panel = Widgets.Panel(Theme);
		Widgets.BottomSheet(panel);
		scrim.AddChild(panel);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 16);
		panel.AddChild(column);
		column.AddChild(Widgets.Label("Board", 30, Theme.TextMuted));
		column.AddChild(MakeChoices(Styles.Choices, board, Styles.BoardName, s => BoardChosen?.Invoke(s)));
		column.AddChild(Widgets.Label(piecesLabel, 30, Theme.TextMuted));
		column.AddChild(MakeChoices(Styles.Choices, pieces, pieceName, s => PiecesChosen?.Invoke(s)));

		var done = Widgets.Button("Done", 30, Theme.Accent, Theme);
		done.CustomMinimumSize = new Vector2(0, 70);
		done.Pressed += () => Widgets.FadeOut(scrim);
		column.AddChild(done);
		return scrim;
	}

	private GridContainer MakeChoices(Style[] choices, Style selected, Func<Style, string> name, Action<Style> chosen)
	{
		var grid = new GridContainer { Columns = 3 };
		grid.AddThemeConstantOverride("h_separation", 12);
		grid.AddThemeConstantOverride("v_separation", 12);
		var group = new ButtonGroup();
		foreach (var choice in choices)
		{
			var button = Widgets.Button(name(choice), 24, Theme.ButtonDark, Theme);
			button.ToggleMode = true;
			button.ButtonGroup = group;
			button.ButtonPressed = choice == selected;
			button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			button.CustomMinimumSize = new Vector2(0, 64);
			button.Pressed += () => chosen(choice);
			grid.AddChild(button);
		}
		return grid;
	}
}
