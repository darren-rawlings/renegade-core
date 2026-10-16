using System;
using Godot;
using Renegade.Dev;
using PoingStudios.AdMob.Api;
using PoingStudios.AdMob.Api.Core;
using PoingStudios.AdMob.Api.Listeners;
using PoingStudios.AdMob.Ump.Api;
using PoingStudios.AdMob.Ump.Core;

namespace Renegade.Ads;

/// <summary>
/// Real Google AdMob ads on Android, via the Poing Studios AdMob plugin (addons/admob).
/// Startup asks for privacy consent where the law requires it (EU/UK), then initialises the SDK
/// and keeps one interstitial and one rewarded ad loaded, so they show without a wait.
/// If an ad isn't ready (offline, no fill), the game carries on instead of getting stuck.
/// </summary>
public sealed class AdMobAds : IAdService
{
	private readonly AdMobConfig _config;

	private InterstitialAd? _interstitial;
	private RewardedAd? _rewarded;
	private bool _rewardedLoading;
	private (Action Rewarded, Action Closed)? _waiting;

	/// <summary>How long a rewarded ad that's still loading is waited for before the reward is just granted.</summary>
	private const double WaitSeconds = 8;
	private bool _ready;

	/// <summary>True when the native AdMob plugin is in this build (Android with the plugin enabled).</summary>
	public static bool Available => Engine.HasSingleton("PoingGodotAdMob");

	public AdMobAds(AdMobConfig config)
	{
		_config = config;
	}

	public void Start()
	{
		var consent = UserMessagingPlatform.ConsentInformation;
		consent.Update(
			new ConsentRequestParameters { TagForUnderAgeOfConsent = _config.ForChildren },
			onSuccess: () =>
			{
				bool formNeeded = consent.GetIsConsentFormAvailable()
					&& consent.GetConsentStatus() == ConsentInformation.ConsentStatus.Required;
				if (formNeeded)
					UserMessagingPlatform.LoadConsentForm(form => form.Show(_ => Initialize()), _ => Initialize());
				else
					Initialize();
			},
			onFailure: _ => Initialize());
	}

	private void Initialize()
	{
		if (_ready)
			return;
		// Games for children (Google Play Families policy) treat every request as child-directed:
		// G-rated, non-personalised, no ad tracking. Otherwise ads are still kept family-friendly.
		MobileAds.SetRequestConfiguration(new RequestConfiguration
		{
			MaxAdContentRating = RequestConfiguration.MaxAdContentRatingG,
			ChildDirectedTreatment = _config.ForChildren
				? RequestConfiguration.TagForChildDirectedTreatment.True
				: RequestConfiguration.TagForChildDirectedTreatment.Unspecified,
			UnderAgeOfConsent = _config.ForChildren
				? RequestConfiguration.TagForUnderAgeOfConsent.True
				: RequestConfiguration.TagForUnderAgeOfConsent.Unspecified,
		});
		MobileAds.Initialize(new OnInitializationCompleteListener
		{
			OnInitializationComplete = _ =>
			{
				_ready = true;
				BlackBox.Write("ad: AdMob initialised");
				LoadInterstitial();
				LoadRewarded();
			},
		});
	}

	public void ShowInterstitial(Action closed)
	{
		var ad = _interstitial;
		if (ad == null)
		{
			BlackBox.Write("ad: interstitial not ready, skipped");
			LoadInterstitial();
			closed();
			return;
		}
		_interstitial = null;
		ad.FullScreenContentCallback = new FullScreenContentCallback
		{
			OnAdDismissedFullScreenContent = () => Finish(ad.Destroy, LoadInterstitial, closed),
			OnAdFailedToShowFullScreenContent = _ => Finish(ad.Destroy, LoadInterstitial, closed),
		};
		BlackBox.Write("ad: interstitial shown");
		ad.Show();
	}

	public void ShowRewarded(Action rewarded, Action closed)
	{
		var ad = _rewarded;
		if (ad == null && _rewardedLoading && _waiting == null)
		{
			// One is on its way (straight after another ad, in a series): wait a little for it.
			BlackBox.Write("ad: rewarded still loading, waiting");
			var waiting = (rewarded, closed);
			_waiting = waiting;
			if (Engine.GetMainLoop() is SceneTree tree)
			{
				tree.CreateTimer(WaitSeconds).Timeout += () =>
				{
					if (_waiting != waiting)
						return;
					_waiting = null;
					BlackBox.Write("ad: rewarded took too long, granting reward anyway");
					rewarded();
					closed();
				};
			}
			return;
		}
		if (ad == null)
		{
			// No ad to show (offline or none available): don't punish the player, grant the undo.
			BlackBox.Write("ad: rewarded not ready, granting reward anyway");
			LoadRewarded();
			rewarded();
			closed();
			return;
		}
		_rewarded = null;
		bool earned = false;
		ad.FullScreenContentCallback = new FullScreenContentCallback
		{
			OnAdDismissedFullScreenContent = () =>
			{
				if (earned)
					rewarded();
				Finish(ad.Destroy, LoadRewarded, closed);
			},
			OnAdFailedToShowFullScreenContent = _ =>
			{
				rewarded();
				Finish(ad.Destroy, LoadRewarded, closed);
			},
		};
		BlackBox.Write("ad: rewarded shown");
		ad.Show(new OnUserEarnedRewardListener { OnUserEarnedReward = _ => earned = true });
	}

	private static void Finish(Action destroy, Action reload, Action closed)
	{
		BlackBox.Write("ad: closed");
		destroy();
		reload();
		closed();
	}

	private void LoadInterstitial()
	{
		if (!_ready)
			return;
		// A new loader every time: the plugin gives each loader one ID, every ad it loads shares it,
		// and old ads stay connected, so reusing a loader makes one dismissal fire every past ad's callbacks.
		new InterstitialAdLoader().Load(_config.InterstitialUnitId, new AdRequest(), new InterstitialAdLoadCallback
		{
			OnAdLoaded = ad => _interstitial = ad,
			OnAdFailedToLoad = error => BlackBox.Write($"ad: interstitial failed to load: {error.Message}"),
		});
	}

	private void LoadRewarded()
	{
		if (!_ready || _rewardedLoading)
			return;
		_rewardedLoading = true;
		// New loader each time, as in LoadInterstitial.
		new RewardedAdLoader().Load(_config.RewardedUnitId, new AdRequest(), new RewardedAdLoadCallback
		{
			OnAdLoaded = ad =>
			{
				_rewarded = ad;
				_rewardedLoading = false;
				if (_waiting is { } waiting)
				{
					_waiting = null;
					ShowRewarded(waiting.Rewarded, waiting.Closed);
				}
			},
			OnAdFailedToLoad = error =>
			{
				_rewardedLoading = false;
				BlackBox.Write($"ad: rewarded failed to load: {error.Message}");
				if (_waiting is { } waiting)
				{
					_waiting = null;
					waiting.Rewarded();
					waiting.Closed();
				}
			},
		});
	}
}
