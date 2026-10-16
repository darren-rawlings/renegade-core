using System;
using System.Collections.Generic;
using Godot;

namespace Renegade.Dev;

/// <summary>
/// A flight recorder kept on the device: a short, always-on log of the events that matter when
/// something goes wrong in a player's hands (sound checks, pauses and resumes, ads). The system
/// log on a phone rolls over within minutes; this file survives until the next few sessions, so
/// it can be read afterwards with
/// `adb exec-out run-as com.renegadegames.GAME cat files/blackbox.log`.
/// </summary>
public static class BlackBox
{
	private const string Path = "user://blackbox.log";
	private const int KeepLines = 1500;

	private static readonly List<string> Pending = new();
	private static bool _started;

	/// <summary>Trims the file to its last lines and marks a new session. Called once at startup.</summary>
	public static void Start(string game)
	{
		if (_started)
			return;
		_started = true;
		var lines = new List<string>();
		if (FileAccess.FileExists(Path))
		{
			using var read = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
			while (read != null && !read.EofReached())
				lines.Add(read.GetLine());
		}
		if (lines.Count > KeepLines)
			lines.RemoveRange(0, lines.Count - KeepLines);
		using var write = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
		foreach (string line in lines)
		{
			if (line.Length > 0)
				write?.StoreLine(line);
		}
		Write($"=== {game} started, Godot {Engine.GetVersionInfo()["string"]}, {OS.GetModelName()}");
	}

	/// <summary>Records an event with the wall-clock time, so it can be matched with what the player saw.</summary>
	public static void Write(string message)
	{
		string line = $"{DateTime.Now:MM-dd HH:mm:ss.fff} {message}";
		DevLog.Write(message);
		lock (Pending)
			Pending.Add(line);
	}

	/// <summary>Appends what's been written since the last flush. Cheap; called a few times a second.</summary>
	public static void Flush()
	{
		string[] lines;
		lock (Pending)
		{
			if (Pending.Count == 0)
				return;
			lines = Pending.ToArray();
			Pending.Clear();
		}
		using var file = FileAccess.Open(Path, FileAccess.ModeFlags.ReadWrite) ?? FileAccess.Open(Path, FileAccess.ModeFlags.Write);
		if (file == null)
			return;
		file.SeekEnd();
		foreach (string line in lines)
			file.StoreLine(line);
	}
}
