using System;
using Godot;

namespace Renegade.Ui;

/// <summary>Interface colours; each game passes its own so the HUD matches its board.</summary>
public sealed record HudTheme(Color Text, Color TextMuted, Color Accent, Color ButtonDark, Color Panel)
{
	/// <summary>Warm wood tones, the studio default.</summary>
	public static HudTheme Warm { get; } = new(new("fff3e0"), new("d9b99b"), new("e0782a"), new("4a2c1a"), new Color("2a180e") with { A = 0.96f });
}

/// <summary>The studio's interface building blocks: labels that read over any board, rounded buttons, panels.</summary>
public static class Widgets
{
	public static Label Label(string text, int fontSize, Color color, bool wrap = false)
	{
		var label = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		// A soft shadow and outline keep text readable over any board or table.
		label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.5f));
		label.AddThemeConstantOverride("shadow_offset_y", 3);
		label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.55f));
		label.AddThemeConstantOverride("outline_size", 8);
		return label;
	}

	/// <summary>Plain body text for panels: no outline, comfortable line spacing.</summary>
	public static Label Body(string text, int fontSize, Color color)
	{
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeConstantOverride("line_spacing", 6);
		return label;
	}

	public static Button Button(string text, int fontSize, Color color, HudTheme theme)
	{
		var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
		button.AddThemeFontSizeOverride("font_size", fontSize);
		Restyle(button, color, theme);
		return button;
	}

	public static void Restyle(Button button, Color color, HudTheme theme)
	{
		foreach (var (state, shade) in new[] { ("normal", color), ("hover", color.Lightened(0.12f)), ("pressed", theme.Accent), ("hover_pressed", theme.Accent.Lightened(0.12f)), ("disabled", color.Darkened(0.3f)) })
		{
			var box = new StyleBoxFlat { BgColor = shade, AntiAliasing = true };
			box.SetCornerRadiusAll(32);
			box.SetContentMarginAll(12);
			button.AddThemeStyleboxOverride(state, box);
		}
	}

	public static PanelContainer Panel(HudTheme theme, int margin = 28)
	{
		var panel = new PanelContainer();
		var box = new StyleBoxFlat { BgColor = theme.Panel, AntiAliasing = true, ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 18 };
		box.SetCornerRadiusAll(28);
		box.SetContentMarginAll(margin);
		box.BorderColor = theme.Accent with { A = 0.35f };
		box.SetBorderWidthAll(2);
		panel.AddThemeStyleboxOverride("panel", box);
		return panel;
	}

	/// <summary>A full-screen dimmed layer that blocks taps on the board.</summary>
	public static ColorRect Scrim(float alpha = 0.45f)
	{
		var scrim = new ColorRect { Color = new Color(0, 0, 0, alpha), MouseFilter = Control.MouseFilterEnum.Stop };
		scrim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		return scrim;
	}

	public static void Anchor(Control control, Control.LayoutPreset preset, float left = 0, float right = 0, float top = 0, float bottom = 0)
	{
		control.SetAnchorsPreset(preset);
		control.OffsetLeft = left;
		control.OffsetRight = right;
		control.OffsetTop = top;
		control.OffsetBottom = bottom;
	}

	/// <summary>Pins a panel to the bottom of the screen, growing upwards to fit what's in it.</summary>
	public static void BottomSheet(Control panel, float margin = 24)
	{
		panel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		panel.OffsetLeft = margin;
		panel.OffsetRight = -margin;
		panel.OffsetTop = -margin;
		panel.OffsetBottom = -margin;
		panel.GrowVertical = Control.GrowDirection.Begin;
	}

	/// <summary>Centres a panel, growing both ways to fit.</summary>
	public static void Centred(Control panel, float width)
	{
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft = -width / 2;
		panel.OffsetRight = width / 2;
		panel.GrowVertical = Control.GrowDirection.Both;
		panel.GrowHorizontal = Control.GrowDirection.Both;
	}

	/// <summary>Fades a control in from transparent, slightly rising: how every pop-up appears.</summary>
	public static void FadeIn(Control control)
	{
		control.Modulate = control.Modulate with { A = 0f };
		control.Visible = true;
		control.CreateTween().TweenProperty(control, "modulate:a", 1f, 0.18f);
	}

	public static void FadeOut(Control control, Action? then = null)
	{
		var tween = control.CreateTween();
		tween.TweenProperty(control, "modulate:a", 0f, 0.14f);
		tween.TweenCallback(Callable.From(() =>
		{
			control.Visible = false;
			then?.Invoke();
		}));
	}
}
