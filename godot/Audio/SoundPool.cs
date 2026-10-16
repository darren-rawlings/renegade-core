using Godot;
using Renegade.Dev;

namespace Renegade.Audio;

/// <summary>
/// A fixed set of positional players reused round-robin, so a burst of sounds (a 42-piece
/// cascade) can't spawn unbounded nodes. When all are busy the oldest sound is cut off, which is
/// inaudible in a clatter.
/// </summary>
public partial class SoundPool : Node3D
{
	public const int DefaultVoices = 20;

	/// <summary>Overall level for all sound effects; keeps big bursts from getting harsh on phone speakers.</summary>
	public const float MasterDb = -6f;

	private readonly AudioStreamPlayer3D[] _players;
	private int _next;

	public SoundPool() : this(DefaultVoices) { }

	public SoundPool(int voices)
	{
		_players = new AudioStreamPlayer3D[voices];
	}

	public override void _Ready()
	{
		for (int i = 0; i < _players.Length; i++)
		{
			_players[i] = MakePlayer();
			AddChild(_players[i]);
		}
	}

	/// <summary>Raised for every sound played, with its level; <see cref="Renegade.Dev.AudioWatch"/> uses it.</summary>
	public static event System.Action<float, int>? Played;

	/// <summary>Plays a sound at a world position. <paramref name="volumeDb"/> is added to <see cref="MasterDb"/>.</summary>
	public void Play(AudioStream stream, Vector3 position, float volumeDb = 0f, float pitch = 1f)
	{
		int voice = _next;
		var player = _players[voice];
		_next = (_next + 1) % _players.Length;
		player.Stream = stream;
		player.GlobalPosition = position;
		player.VolumeDb = volumeDb + MasterDb;
		player.PitchScale = pitch;
		player.Play();
		Played?.Invoke(volumeDb, voice);
	}

	/// <summary>
	/// Plays a struck-object sound. Strength is 0..1 (how hard it hit); light taps come out quieter
	/// and slightly higher, as with real objects. <paramref name="gainDb"/> evens out loud materials.
	/// </summary>
	public void PlayImpact(AudioStream stream, Vector3 position, float strength, float gainDb = 0f)
	{
		float volume = Mathf.LinearToDb(Mathf.Clamp(strength, 0.02f, 1f)) + gainDb;
		float pitch = (float)GD.RandRange(0.95, 1.05) + (1f - strength) * 0.06f;
		DevLog.Write($"impact strength={strength:0.00}");
		Play(stream, position, volume, pitch);
	}

	/// <summary>
	/// Positional player with distance attenuation turned off: in a phone game the camera stays at
	/// about the same distance, and only left/right panning is wanted. The distance low-pass filter
	/// is off too; by default it muffles everything above 5 kHz, which takes the ring out of metal
	/// and glass.
	/// </summary>
	public static AudioStreamPlayer3D MakePlayer() => new()
	{
		AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled,
		AttenuationFilterDb = 0f,
		PanningStrength = 0.6f,
	};
}
