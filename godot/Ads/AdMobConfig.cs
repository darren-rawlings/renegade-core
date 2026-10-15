namespace Renegade.Ads;

/// <summary>
/// One game's AdMob settings. The App ID itself goes in Project Settings
/// (admob/general/android/app_id); this holds what the code needs at runtime.
/// </summary>
public sealed record AdMobConfig(string InterstitialUnitId, string RewardedUnitId, bool ForChildren = true)
{
	/// <summary>
	/// Google's public test ad units: clearly labelled test ads that never pay out and are safe to
	/// tap. Use these until the game's real units exist, and never ship them.
	/// </summary>
	public static AdMobConfig Test { get; } = new(
		InterstitialUnitId: "ca-app-pub-3940256099942544/1033173712",
		RewardedUnitId: "ca-app-pub-3940256099942544/5224354917");

	public bool IsTest => InterstitialUnitId == Test.InterstitialUnitId || RewardedUnitId == Test.RewardedUnitId;
}
