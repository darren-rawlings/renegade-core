using Godot;
using Renegade.Audio;

namespace Renegade.Dev;

/// <summary>
/// Checks, in the player's hands, that every sound the game plays really reaches the game's output:
/// after each sound it looks at the master bus level. If sounds go missing there, the game itself
/// went quiet; if they're fine there but the player heard nothing, Android or the phone silenced a
/// working game. Either way the result goes to the <see cref="BlackBox"/>, with app pauses, resumes
/// and focus changes, which are the usual suspects.
/// </summary>
public partial class AudioWatch : Node
{
	/// <summary>How long after a sound to look for it on the bus. Phones buffer audio for a while, so this is generous.</summary>
	private const float CheckDelay = 0.35f;

	/// <summary>Below this the bus counts as silent. A quiet tap peaks around -35 dB.</summary>
	private const float SilentDb = -60f;

	private readonly System.Collections.Generic.Queue<(float Due, float Db, int Voice, float Started)> _due = new();
	private int _details;
	private float _clock;
	private float _loudest = -200f;
	private int _played;
	private int _heard;
	private int _silent;
	private float _reportTimer;
	private float _flushTimer;

	public static void Attach(Node parent, string game)
	{
		BlackBox.Start(game);
		parent.AddChild(new AudioWatch());
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SoundPool.Played += OnPlayed;
		BlackBox.Write($"audio: output '{AudioServer.OutputDevice}', mix {AudioServer.GetMixRate()} Hz, bus muted={AudioServer.IsBusMute(0)}, latency {AudioServer.GetOutputLatency() * 1000:0} ms");
	}

	public override void _ExitTree()
	{
		SoundPool.Played -= OnPlayed;
		Report();
		BlackBox.Flush();
	}

	private void OnPlayed(float volumeDb, int voice)
	{
		// Very quiet taps (tumbling pieces) can sit under the threshold; only judge real hits.
		if (volumeDb < -24f)
			return;
		_played++;
		_due.Enqueue((_clock + CheckDelay, volumeDb, voice, _clock));
	}

	public override void _Process(double delta)
	{
		_clock += (float)delta;
		float peak = Mathf.Max(AudioServer.GetBusPeakVolumeLeftDb(0, 0), AudioServer.GetBusPeakVolumeRightDb(0, 0));
		_loudest = Mathf.Max(_loudest, peak);
		while (_due.Count > 0 && _due.Peek().Due <= _clock)
		{
			var check = _due.Dequeue();
			if (AudioServer.IsBusMute(0))
				continue;
			if (_loudest > SilentDb)
			{
				_heard++;
			}
			else
			{
				_silent++;
				// The first few silent sounds in a session in detail, to spot a pattern.
				if (_details++ < 25)
					BlackBox.Write($"audio: silent sound on voice {check.Voice}, level {check.Db:0} dB, at {check.Started:0.00} s");
			}
			if (_due.Count == 0)
				_loudest = -200f;
		}
		if (_due.Count == 0)
			_loudest = -200f;

		_reportTimer += (float)delta;
		if (_reportTimer >= 15f)
		{
			_reportTimer = 0f;
			Report();
		}
		_flushTimer += (float)delta;
		if (_flushTimer >= 2f)
		{
			_flushTimer = 0f;
			BlackBox.Flush();
		}
	}

	private void Report()
	{
		if (_played == 0)
			return;
		BlackBox.Write($"audio: {_played} sounds, {_heard} reached the output, {_silent} silent at the engine");
		_played = _heard = _silent = 0;
	}

	public override void _Notification(int what)
	{
		string? name = (long)what switch
		{
			NotificationApplicationPaused => "app paused",
			NotificationApplicationResumed => "app resumed",
			NotificationApplicationFocusOut => "focus lost",
			NotificationApplicationFocusIn => "focus back",
			NotificationWMWindowFocusOut => "window focus lost",
			NotificationWMWindowFocusIn => "window focus back",
			_ => null,
		};
		if (name == null)
			return;
		Report();
		BlackBox.Write($"{name}; bus muted={AudioServer.IsBusMute(0)}");
		BlackBox.Flush();
	}
}
