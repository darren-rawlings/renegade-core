using System;
using System.Collections.Generic;
using Godot;
using Renegade.Core;
using Renegade.Looks;

namespace Renegade.Play;

/// <summary>
/// Player preferences, saved on the device only (the privacy policy promises nothing leaves it).
/// The common ones are properties; a game can keep its own extras in <see cref="Extra"/>.
/// </summary>
public sealed class GameSettings
{
	private const string Path = "user://settings.cfg";
	private const string Section = "settings";

	public bool Muted { get; set; }
	public GameMode Mode { get; set; } = GameMode.PlayerVsComputer;
	public Difficulty Difficulty { get; set; } = Difficulty.Easy;
	public Style Board { get; set; } = Style.Wood;
	public Style Pieces { get; set; } = Style.Glass;

	/// <summary>Finished games since the last between-games ad; see <see cref="AdSchedule"/>.</summary>
	public int GamesSinceAd { get; set; }

	public Dictionary<string, string> Extra { get; } = new();

	public static GameSettings Load(Style board = Style.Wood, Style pieces = Style.Glass)
	{
		var settings = new GameSettings { Board = board, Pieces = pieces };
		var file = new ConfigFile();
		if (file.Load(Path) != Error.Ok)
			return settings;
		settings.Muted = file.GetValue(Section, "muted", false).AsBool();
		settings.Mode = ReadEnum(file, "mode", settings.Mode);
		settings.Difficulty = ReadEnum(file, "difficulty", settings.Difficulty);
		settings.Board = ReadStyle(file, "board", settings.Board);
		settings.Pieces = ReadStyle(file, "pieces", settings.Pieces);
		settings.GamesSinceAd = file.GetValue(Section, "games_since_ad", 0).AsInt32();
		if (file.HasSection("extra"))
		{
			foreach (string key in file.GetSectionKeys("extra"))
				settings.Extra[key] = file.GetValue("extra", key).AsString();
		}
		return settings;
	}

	public void Save()
	{
		var file = new ConfigFile();
		file.SetValue(Section, "muted", Muted);
		file.SetValue(Section, "mode", Mode.ToString());
		file.SetValue(Section, "difficulty", Difficulty.ToString());
		file.SetValue(Section, "board", Board.ToString());
		file.SetValue(Section, "pieces", Pieces.ToString());
		file.SetValue(Section, "games_since_ad", GamesSinceAd);
		foreach (var (key, value) in Extra)
			file.SetValue("extra", key, value);
		file.Save(Path);
	}

	private static Style ReadStyle(ConfigFile file, string key, Style fallback)
	{
		var style = ReadEnum(file, key, fallback);
		return style == Style.Table ? fallback : style;
	}

	private static T ReadEnum<T>(ConfigFile file, string key, T fallback) where T : struct, Enum =>
		Enum.TryParse(file.GetValue(Section, key, fallback.ToString()).AsString(), out T value) ? value : fallback;
}
