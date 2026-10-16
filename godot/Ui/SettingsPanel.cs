using System;
using Godot;

namespace Renegade.Ui;

/// <summary>
/// The Settings sheet: a panel that slides up from the bottom with one row per setting. Sound and
/// Freeze board come built in; each game adds its own rows with <see cref="AddToggle"/> and
/// <see cref="AddChoice"/>.
/// </summary>
public partial class SettingsPanel : Control
{
	private readonly HudTheme _theme;
	private readonly VBoxContainer _rows = new();

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

		var panel = Widgets.Panel(theme);
		Widgets.BottomSheet(panel);
		AddChild(panel);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 18);
		panel.AddChild(column);
		column.AddChild(Widgets.Label("Settings", 34, theme.Text));
		_rows.AddThemeConstantOverride("separation", 14);
		column.AddChild(_rows);
		var done = Widgets.Button("Done", 30, theme.Accent, theme);
		done.CustomMinimumSize = new Vector2(0, 70);
		done.Pressed += Close;
		column.AddChild(done);
		Visible = false;
	}

	public void Open() => Widgets.FadeIn(this);

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
