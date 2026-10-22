using System;
using System.Collections.Generic;
using Godot;

namespace Renegade.Ui;

/// <summary>One step of a tutorial: a heading, a short explanation, and a looping clip that shows it.</summary>
public sealed record TutorialPage(string Title, string Text, string? Video = null);

/// <summary>
/// The guided tour. First an invitation ("Learn to play?" with Start / Not now), then a card per
/// step: a looping video clip recorded from the game itself, framed with rounded corners, a step
/// counter, the heading and text, progress dots, and Back / Next. Steps cross-fade into each other.
/// Text only, no voice.
/// </summary>
public partial class TutorialPanel : Control
{
	private readonly HudTheme _theme;
	private readonly string _game;
	private readonly IReadOnlyList<TutorialPage> _pages;
	private Control _offer = null!;
	private Control _tour = null!;
	private Control _content = null!;
	private Control _videoFrame = null!;
	private VideoStreamPlayer _video = null!;
	private Label _counter = null!;
	private Label _title = null!;
	private Label _text = null!;
	private HBoxContainer _dots = null!;
	private Button _back = null!;
	private Button _next = null!;
	private int _page;

	public event Action? Closed;
	public event Action? Opened;

	public TutorialPanel(HudTheme theme, string game, IReadOnlyList<TutorialPage> pages)
	{
		_theme = theme;
		_game = game;
		_pages = pages;
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		AddChild(Widgets.Scrim(0.72f));
		BuildOffer();
		BuildTour();
		Visible = false;
	}

	/// <summary>Shows the invitation first.</summary>
	public void Offer()
	{
		_tour.Visible = false;
		_offer.Visible = true;
		Widgets.FadeIn(this);
		Opened?.Invoke();
	}

	private void BuildOffer()
	{
		var panel = Widgets.Panel(_theme, 36);
		Widgets.Centred(panel, 620);
		_offer = panel;
		AddChild(panel);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 20);
		panel.AddChild(column);
		column.AddChild(Widgets.Label("How to play", 26, _theme.TextMuted));
		column.AddChild(Widgets.Label(_game, 46, _theme.Text));
		column.AddChild(Widgets.Body($"A short guided tour with video: {_pages.Count} steps, about a minute. You can leave it any time.", 26, _theme.TextMuted));
		var start = Widgets.Button("Start the tour", 30, _theme.Accent, _theme);
		start.CustomMinimumSize = new Vector2(0, 76);
		start.Pressed += () => Widgets.FadeOut(_offer, () => StartTour());
		column.AddChild(start);
		var later = Widgets.Button("Not now", 26, _theme.ButtonDark, _theme);
		later.CustomMinimumSize = new Vector2(0, 64);
		later.Pressed += Close;
		column.AddChild(later);
	}

	private void BuildTour()
	{
		var panel = Widgets.Panel(_theme, 28);
		Widgets.Anchor(panel, LayoutPreset.FullRect, left: 22, right: -22, top: 70, bottom: -40);
		_tour = panel;
		AddChild(panel);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 16);
		panel.AddChild(column);

		var header = new HBoxContainer();
		_counter = Widgets.Body("", 22, _theme.TextMuted);
		_counter.HorizontalAlignment = HorizontalAlignment.Left;
		_counter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(_counter);
		var skip = Widgets.Button("Close", 20, _theme.ButtonDark, _theme);
		skip.CustomMinimumSize = new Vector2(110, 48);
		skip.Pressed += Close;
		header.AddChild(skip);
		column.AddChild(header);

		_content = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		_content.AddThemeConstantOverride("separation", 16);
		column.AddChild(_content);

		// The clip, in a frame with rounded corners: the frame's own shape clips the video.
		var frame = new PanelContainer { ClipChildren = ClipChildrenMode.Only, CustomMinimumSize = new Vector2(0, 560) };
		var frameBox = new StyleBoxFlat { BgColor = new Color("0d0806"), AntiAliasing = true };
		frameBox.SetCornerRadiusAll(22);
		frame.AddThemeStyleboxOverride("panel", frameBox);
		_video = new VideoStreamPlayer { Expand = true, Loop = true, Autoplay = false, MouseFilter = MouseFilterEnum.Ignore };
		var aspect = new AspectRatioContainer { Ratio = 1f };
		aspect.AddChild(_video);
		frame.AddChild(aspect);
		_videoFrame = frame;
		_content.AddChild(frame);

		_title = Widgets.Label("", 36, _theme.Text);
		_content.AddChild(_title);
		_text = Widgets.Body("", 26, _theme.TextMuted);
		_content.AddChild(_text);

		_dots = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_dots.AddThemeConstantOverride("separation", 10);
		column.AddChild(_dots);

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 14);
		_back = Widgets.Button("Back", 26, _theme.ButtonDark, _theme);
		_back.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_back.CustomMinimumSize = new Vector2(0, 72);
		_back.Pressed += () => Go(_page - 1);
		buttons.AddChild(_back);
		_next = Widgets.Button("Next", 28, _theme.Accent, _theme);
		_next.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_next.CustomMinimumSize = new Vector2(0, 72);
		_next.Pressed += () =>
		{
			if (_page + 1 < _pages.Count)
				Go(_page + 1);
			else
				Close();
		};
		buttons.AddChild(_next);
		column.AddChild(buttons);
	}

	/// <summary>Opens the tour straight at a step (for checking the pages).</summary>
	public void OpenAt(int page)
	{
		_offer.Visible = false;
		Visible = true;
		Modulate = Colors.White;
		_page = Mathf.Clamp(page, 0, _pages.Count - 1);
		ShowPage();
		_tour.Visible = true;
	}

	private void StartTour()
	{
		_page = 0;
		ShowPage();
		Widgets.FadeIn(_tour);
	}

	private void Go(int page)
	{
		if (page < 0 || page >= _pages.Count)
			return;
		var fade = CreateTween();
		fade.TweenProperty(_content, "modulate:a", 0f, 0.14f);
		fade.TweenCallback(Callable.From(() =>
		{
			_page = page;
			ShowPage();
		}));
		fade.TweenProperty(_content, "modulate:a", 1f, 0.2f);
	}

	private void ShowPage()
	{
		var page = _pages[_page];
		_counter.Text = $"Step {_page + 1} of {_pages.Count}";
		_title.Text = page.Title;
		_text.Text = page.Text;
		_back.Disabled = _page == 0;
		_next.Text = _page + 1 < _pages.Count ? "Next" : "Let's play";
		_video.Stop();
		bool hasVideo = page.Video != null && ResourceLoader.Exists(page.Video);
		_videoFrame.Visible = hasVideo;
		if (hasVideo)
		{
			_video.Stream = GD.Load<VideoStream>(page.Video);
			_video.Play();
		}
		foreach (var dot in _dots.GetChildren())
			dot.QueueFree();
		for (int i = 0; i < _pages.Count; i++)
		{
			var dot = new Panel { CustomMinimumSize = new Vector2(i == _page ? 30 : 12, 12) };
			var box = new StyleBoxFlat { BgColor = i == _page ? _theme.Accent : _theme.TextMuted with { A = 0.4f }, AntiAliasing = true };
			box.SetCornerRadiusAll(6);
			dot.AddThemeStyleboxOverride("panel", box);
			_dots.AddChild(dot);
		}
	}

	public void Close()
	{
		_video.Stop();
		Widgets.FadeOut(this, () => Closed?.Invoke());
	}
}
