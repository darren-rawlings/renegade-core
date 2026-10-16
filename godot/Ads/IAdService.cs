using System;

namespace Renegade.Ads;

/// <summary>
/// Shows ads. The game only talks to this interface, so the on-screen stand-in used on desktop
/// and the real AdMob ads on phones are interchangeable.
/// </summary>
public interface IAdService
{
	/// <summary>Full-screen ad at a natural break. <paramref name="closed"/> runs when it's gone (or failed).</summary>
	void ShowInterstitial(Action closed);

	/// <summary>
	/// Opt-in ad that grants a reward. <paramref name="rewarded"/> runs only if the player watched it
	/// through; <paramref name="closed"/> always runs afterwards.
	/// </summary>
	void ShowRewarded(Action rewarded, Action closed);

	/// <summary>
	/// Several rewarded ads back to back, for a bigger reward. <paramref name="rewarded"/> runs once,
	/// only if every ad was watched through; skipping one ends the series. <paramref name="closed"/>
	/// always runs at the end.
	/// </summary>
	void ShowRewardedSeries(int count, Action rewarded, Action closed)
	{
		if (count <= 0)
		{
			rewarded();
			closed();
			return;
		}
		bool earned = false;
		ShowRewarded(() => earned = true, () =>
		{
			if (earned)
				ShowRewardedSeries(count - 1, rewarded, closed);
			else
				closed();
		});
	}
}
