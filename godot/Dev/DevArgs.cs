using System.Collections.Generic;
using Godot;

namespace Renegade.Dev;

/// <summary>
/// Developer flags: the user args after `--` on the command line, plus any in a one-shot
/// user://dev_args.txt (one per line). Phones can't pass launch arguments, so tests push that
/// file over adb instead; it is deleted after reading so the next normal launch is unaffected.
/// </summary>
public static class DevArgs
{
	public const string FilePath = "user://dev_args.txt";

	/// <summary>Each flag split at the first '=': `--board=Wood` gives ("--board", "Wood"), `--log` gives ("--log", "").</summary>
	public static IEnumerable<(string Name, string Value)> Read()
	{
		foreach (string arg in Raw())
		{
			var parts = arg.Split('=', 2);
			yield return (parts[0], parts.Length > 1 ? parts[1] : "");
		}
	}

	private static IEnumerable<string> Raw()
	{
		foreach (string arg in OS.GetCmdlineUserArgs())
			yield return arg;
		if (!FileAccess.FileExists(FilePath))
			yield break;
		string text = FileAccess.GetFileAsString(FilePath);
		DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(FilePath));
		foreach (string line in text.Split('\n'))
		{
			if (line.Trim() is { Length: > 0 } arg)
				yield return arg;
		}
	}
}
