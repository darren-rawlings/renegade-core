using System;
using Godot;

namespace Renegade.Ui;

/// <summary>
/// The Settings sheet: a panel that slides up from the bottom with one row per setting. Sound and
/// Freeze board come built in; each game adds its own rows with <see cref="AddToggle"/> and
/// <see cref="AddChoice"/>, and the HUD adds the shop with <see cref="AddActions"/>. A long list
/// scrolls.
/// </summary>
public partial class SettingsPanel : Control
{
	private readonly HudTheme _theme;
	private readonly VBoxContainer _rows = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
	private readonly ScrollContainer _scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };

	/// <summary>Rows that always come last, after every game's own (the shop).</summary>
	private readonly VBoxContainer _footer = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
	private readonly VBoxContainer _content = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
	private readonly PanelContainer _panel;

	public SettingsPanel(HudTheme theme)
	{
		_theme = theme;
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		var scrim = Widgets.Scrim(0.35f);
		scrim.GuiInput += input =>
		{
			if (input is InputEventMouseButton { Pressed: true })
				Close();
		};
		AddChild(scrim);

		_panel = Widgets.Panel(theme);
		Widgets.BottomSheet(_panel);
		AddChild(_panel);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 18);
		_panel.AddChild(column);
		column.AddChild(Widgets.Label("Settings", 34, theme.Text));
		_rows.AddThemeConstantOverride("separation", 14);
		_footer.AddThemeConstantOverride("separation", 14);
		_content.AddThemeConstantOverride("separation", 14);
		_content.AddChild(_rows);
		_content.AddChild(_footer);
		_scroll.AddChild(_content);
		column.AddChild(_scroll);
		var done = Widgets.Button("Done", 30, theme.Accent, theme);
		done.CustomMinimumSize = new Vector2(0, 70);
		done.Pressed += Close;
		column.AddChild(done);
		Visible = false;
	}

	public async void Open()
	{
		_scroll.ScrollVertical = 0;
		FitHeight();
		Widgets.FadeIn(this);
		// Wrapped text only knows how tall it is once it has a width: fit again after the layout.
		for (int frame = 0; frame < 2; frame++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		FitHeight();
	}

	/// <summary>As tall as the rows, up to about two thirds of the screen; beyond that they scroll.</summary>
	private void FitHeight()
	{
		float wanted = _content.GetCombinedMinimumSize().Y;
		_scroll.CustomMinimumSize = new Vector2(0, Mathf.Min(wanted, GetViewportRect().Size.Y * 0.62f));
		// Let the sheet shrink to fit as well as grow (it grows upward from the bottom edge).
		Widgets.BottomSheet(_panel);
	}

	public void Close() => Widgets.FadeOut(this);

	/// <summary>A row with a label and an On/Off switch.</summary>
	public void AddToggle(string label, bool value, Action<bool> changed)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);
		var text = Widgets.Body(label, 26, _theme.Text);
		text.HorizontalAlignment = HorizontalAlignment.Left;
		text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		text.VerticalAlignment = VerticalAlignment.Center;
		row.AddChild(text);
		var toggle = Widgets.Button("", 24, _theme.ButtonDark, _theme);
		toggle.CustomMinimumSize = new Vector2(130, 60);
		void Show()
		{
			toggle.Text = value ? "On" : "Off";
			Widgets.Restyle(toggle, value ? _theme.Accent : _theme.ButtonDark, _theme);
		}
		toggle.Pressed += () =>
		{
			value = !value;
			Show();
			changed(value);
		};
		Show();
		row.AddChild(toggle);
		_rows.AddChild(row);
	}

	/// <summary>A small heading between groups of rows. Returned so it can be hidden.</summary>
	public Label AddHeading(string text, bool footer = false)
	{
		var label = Widgets.Body(text, 24, _theme.TextMuted);
		label.HorizontalAlignment = HorizontalAlignment.Left;
		(footer ? _footer : _rows).AddChild(label);
		return label;
	}

	/// <summary>
	/// A row of buttons that do something (buy, support) rather than change a setting. The buttons
	/// are returned so their text, colour or visibility can change later.
	/// </summary>
	public Button[] AddActions(bool footer, params (string Text, bool Accent, Action Pressed)[] actions)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		var buttons = new Button[actions.Length];
		for (int i = 0; i < actions.Length; i++)
		{
			var (text, accent, pressed) = actions[i];
			var button = Widgets.Button(text, 22, accent ? _theme.Accent : _theme.ButtonDark, _theme);
			button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			button.CustomMinimumSize = new Vector2(0, 58);
			button.Pressed += pressed;
			buttons[i] = button;
			row.AddChild(button);
		}
		(footer ? _footer : _rows).AddChild(row);
		return buttons;
	}

	/// <summary>A line of plain text (a note under a group of rows). Returned so it can change.</summary>
	public Label AddNote(string text, bool footer = false)
	{
		var label = Widgets.Body(text, 20, _theme.TextMuted);
		label.HorizontalAlignment = HorizontalAlignment.Left;
		(footer ? _footer : _rows).AddChild(label);
		return label;
	}

	/// <summary>A labelled row of options, one chosen at a time (board sizes, marble shapes...).</summary>
	public void AddChoice(string label, string[] options, int selected, Action<int> chosen)
	{
		var text = Widgets.Body(label, 26, _theme.TextMuted);
		text.HorizontalAlignment = HorizontalAlignment.Left;
		_rows.AddChild(text);
		var grid = new GridContainer { Columns = Math.Min(options.Length, options.Length > 4 ? 5 : 4) };
		grid.AddThemeConstantOverride("h_separation", 10);
		grid.AddThemeConstantOverride("v_separation", 10);
		var buttons = new Button[options.Length];
		void Show()
		{
			for (int i = 0; i < buttons.Length; i++)
				Widgets.Restyle(buttons[i], i == selected ? _theme.Accent : _theme.ButtonDark, _theme);
		}
		for (int i = 0; i < options.Length; i++)
		{
			int index = i;
			var button = Widgets.Button(options[i], 22, _theme.ButtonDark, _theme);
			button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			button.CustomMinimumSize = new Vector2(0, 58);
			button.Pressed += () =>
			{
				selected = index;
				Show();
				chosen(index);
			};
			buttons[i] = button;
			grid.AddChild(button);
		}
		Show();
		_rows.AddChild(grid);
	}
}
