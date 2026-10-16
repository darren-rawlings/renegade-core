using System;
using Godot;

namespace Renegade.Ui;

/// <summary>A choice that needs the player's say-so: a title, a sentence or two, and a stack of buttons.</summary>
public static class ConfirmDialog
{
	/// <summary>Shows the dialog over <paramref name="parent"/>. Any button closes it, then runs its action.</summary>
	public static Control Show(Node parent, HudTheme theme, string title, string message, params (string Text, bool Accent, Action? Pressed)[] buttons)
	{
		var root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(Widgets.Scrim(0.5f));
		var panel = Widgets.Panel(theme, 32);
		Widgets.Centred(panel, 600);
		root.AddChild(panel);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 18);
		panel.AddChild(column);
		column.AddChild(Widgets.Label(title, 36, theme.Text));
		column.AddChild(Widgets.Body(message, 26, theme.TextMuted));
		foreach (var (text, accent, pressed) in buttons)
		{
			var button = Widgets.Button(text, 26, accent ? theme.Accent : theme.ButtonDark, theme);
			button.CustomMinimumSize = new Vector2(0, 68);
			button.Pressed += () => Widgets.FadeOut(root, () =>
			{
				root.QueueFree();
				pressed?.Invoke();
			});
			column.AddChild(button);
		}
		parent.AddChild(root);
		Widgets.FadeIn(root);
		return root;
	}
}
