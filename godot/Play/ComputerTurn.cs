using System;
using System.Threading.Tasks;
using Renegade.Dev;

namespace Renegade.Play;

/// <summary>
/// Runs the computer's search on a background thread, so even a deep search never stalls a frame,
/// and hands the move over once a short natural pause has passed. <see cref="Cancel"/> (after a
/// reset or undo) discards a search still running.
/// </summary>
public sealed class ComputerTurn
{
	private Task<(int Generation, int Move)>? _thinking;
	private int _generation;
	private float _elapsed;

	public float MinThinkTime { get; set; } = 0.7f;

	public bool Thinking => _thinking != null;

	/// <summary>Starts a search. <paramref name="search"/> runs off the main thread: give it a copy of the game.</summary>
	public void Start(Func<int> search, string label = "computer")
	{
		int generation = _generation;
		_elapsed = 0f;
		_thinking = Task.Run(() =>
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			int move = search();
			DevLog.Write($"{label} chose {move} in {watch.ElapsedMilliseconds} ms");
			return (generation, move);
		});
	}

	/// <summary>Call every frame; returns the chosen move once it's ready and the pause is over.</summary>
	public int? Poll(float dt)
	{
		if (_thinking == null)
			return null;
		_elapsed += dt;
		if (!_thinking.IsCompleted || _elapsed < MinThinkTime)
			return null;
		var (generation, move) = _thinking.Result;
		_thinking = null;
		return generation == _generation ? move : null;
	}

	public void Cancel()
	{
		_generation++;
		_thinking = null;
	}
}
