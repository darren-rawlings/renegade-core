using Godot;
using Renegade.Store;

namespace Renegade.Ads;

public static class AdServices
{
	/// <summary>
	/// Real AdMob ads when the plugin is in the build (Android), otherwise the on-screen stand-in,
	/// so the whole ad flow works on desktop too. Demo runs always get the stand-in, closing itself.
	/// It also opens the shop: once the player has bought Remove ads, no ad shows at all.
	/// </summary>
	/// <param name="host">Where the stand-in is added; it draws on its own canvas layer.</param>
	public static IAdService Create(Node host, AdMobConfig config, bool demo = false, PlaceholderAds? placeholder = null)
	{
		var shop = Shop.Attach(host);
		if (AdMobAds.Available && !demo && !shop.AdsRemoved)
		{
			var admob = new AdMobAds(config);
			admob.Start();
			return new AdFreeAds(admob);
		}
		placeholder ??= new PlaceholderAds();
		placeholder.AutoClose = demo;
		host.AddChild(placeholder);
		return new AdFreeAds(placeholder);
	}
}
