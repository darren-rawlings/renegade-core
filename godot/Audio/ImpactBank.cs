using System;
using System.Collections.Generic;
using Godot;
using Renegade.Core.Synth;

namespace Renegade.Audio;

/// <summary>
/// Synthesises each kind of impact the first time it's needed, in a few variations so repeated
/// hits don't sound identical, then reuses them. The key is whatever the game sounds differ by,
/// such as a material.
/// </summary>
public sealed class ImpactBank<TKey> where TKey : notnull
{
	private readonly Func<TKey, ImpactRecipe> _recipeFor;
	private readonly int _variations;
	private readonly Dictionary<TKey, AudioStreamWav[]> _clips = new();

	public ImpactBank(Func<TKey, ImpactRecipe> recipeFor, int variations = 4)
	{
		_recipeFor = recipeFor;
		_variations = variations;
	}

	public AudioStreamWav Random(TKey key)
	{
		if (!_clips.TryGetValue(key, out var clips))
		{
			var recipe = _recipeFor(key);
			int seedBase = EqualityComparer<TKey>.Default.GetHashCode(key) * 100;
			clips = new AudioStreamWav[_variations];
			for (int i = 0; i < clips.Length; i++)
				clips[i] = ToStream(ImpactSynth.Impact(recipe, seed: seedBase + i));
			_clips[key] = clips;
		}
		return clips[GD.RandRange(0, clips.Length - 1)];
	}

	/// <summary>Wraps synthesised samples as a playable 16-bit mono stream.</summary>
	public static AudioStreamWav ToStream(float[] samples) => new()
	{
		Format = AudioStreamWav.FormatEnum.Format16Bits,
		MixRate = ImpactSynth.SampleRate,
		Stereo = false,
		Data = ImpactSynth.ToPcm16(samples),
	};
}
