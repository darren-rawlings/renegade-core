namespace Renegade.Core;

public enum GameMode
{
	PlayerVsPlayer,
	PlayerVsComputer,
}

/// <summary>Computer strength. Each game decides what the levels mean for its own search.</summary>
public enum Difficulty
{
	Easy,
	Medium,
	Hard,
}
