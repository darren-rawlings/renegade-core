using System;
using Renegade.Store;

namespace Renegade.Ads;

/// <summary>
/// Wraps the real ads. Once the player has bought Remove ads (<see cref="Shop.AdsRemoved"/>), no ad
/// ever shows: a between-games break passes straight on, and every reward (Undo, Hint, Solve,
/// starting over without a loss) is simply given.
/// </summary>
public sealed class AdFreeAds : IAdService
{
	private readonly IAdService _inner;

	public AdFreeAds(IAdService inner)
	{
		_inner = inner;
	}

	private static bool Off => Shop.Current?.AdsRemoved == true;

	public void ShowInterstitial(Action closed)
	{
		if (Off)
			closed();
		else
			_inner.ShowInterstitial(closed);
	}

	public void ShowRewarded(Action rewarded, Action closed)
	{
		if (Off)
		{
			rewarded();
			closed();
		}
		else
		{
			_inner.ShowRewarded(rewarded, closed);
		}
	}
}
