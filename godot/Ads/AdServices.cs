using Godot;

namespace Renegade.Ads;

public static class AdServices
{
	/// <summary>
	/// Real AdMob ads when the plugin is in the build (Android), otherwise the on-screen stand-in,
	/// so the whole ad flow works on desktop too. Demo runs always get the stand-in, closing itself.
	/// </summary>
	/// <param name="host">Where the stand-in is added; it draws on its own canvas layer.</param>
	public static IAdService Create(Node host, AdMobConfig config, bool demo = false, PlaceholderAds? placeholder = null)
	{
		if (AdMobAds.Available && !demo)
		{
			var admob = new AdMobAds(config);
			admob.Start();
			return admob;
		}
		placeholder ??= new PlaceholderAds();
		placeholder.AutoClose = demo;
		host.AddChild(placeholder);
		return placeholder;
	}
}
