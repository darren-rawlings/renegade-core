using Godot;

namespace Renegade.Dev;

/// <summary>
/// Timestamped developer log, off unless the game is launched with `--log`. Used to diagnose
/// timing and sound problems on real devices, where the output shows up in `adb logcat -s godot`.
/// </summary>
public static class DevLog
{
	public static bool Enabled { get; set; }

	/// <summary>Short game tag at the start of each line, so logs from different games are easy to tell apart.</summary>
	public static string Tag { get; set; } = "dev";

	public static void Write(string message)
	{
		if (Enabled)
			GD.Print($"[{Tag} {Time.GetTicksMsec()}] {message}");
	}
}
