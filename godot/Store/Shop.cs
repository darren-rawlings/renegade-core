using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Renegade.Dev;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Renegade.Store;

/// <summary>
/// The in-app purchases, through Google Play billing (the GodotGooglePlayBilling plugin in
/// addons/GodotGooglePlayBilling). Each game sells the same products, created in its own Play
/// Console listing:
/// - <see cref="RemoveAds"/>: a one-time purchase that turns every ad off (bought once, kept on
///   reinstall: Google Play remembers it, and it's cached here for when the phone is offline);
/// - the <see cref="Tips"/>: "support the game" in a few fixed amounts (Play only sells fixed
///   prices), consumed at once so they can be given again.
/// Nothing shows until Google Play has answered with the products and their local prices, so the
/// buttons stay hidden on desktop, offline, and until the products exist in the Play Console.
/// For desktop testing, `--shop` fakes the store (`--shop=bought`: as if Remove ads were bought).
/// </summary>
public partial class Shop : Node
{
	public const string RemoveAds = "remove_ads";
	public static readonly string[] Tips = { "tip_small", "tip_medium", "tip_large" };

	private const string PluginName = "GodotGooglePlayBilling";
	private const string CachePath = "user://purchases.cfg";
	private const int Ok = 0;
	private const int Purchased = 1;
	private const int Pending = 2;

	private readonly Dictionary<string, string> _prices = new();
	private GodotObject? _billing;
	private bool _fake;

	/// <summary>The shop for this run (one per game); null until <see cref="Attach"/>.</summary>
	public static Shop? Current { get; private set; }

	/// <summary>True once the player has bought Remove ads.</summary>
	public bool AdsRemoved { get; private set; }

	/// <summary>True when Google Play has given us the products and their prices.</summary>
	public bool Open => _prices.ContainsKey(RemoveAds) || Tips.Any(_prices.ContainsKey);

	/// <summary>Anything changed: prices arrived, ads were removed, a purchase is pending.</summary>
	public event Action? Changed;

	/// <summary>A message for the player: thanks for a tip, a purchase waiting to clear, an error.</summary>
	public event Action<string>? Message;

	/// <summary>The price in the player's currency, as Google Play formats it, or null if not for sale.</summary>
	public string? Price(string product) => _prices.TryGetValue(product, out var price) ? price : null;

	/// <summary>Creates the shop once, under <paramref name="host"/>, and connects to Google Play.</summary>
	public static Shop Attach(Node host)
	{
		if (Current != null && IsInstanceValid(Current))
			return Current;
		var shop = new Shop { Name = "Shop" };
		host.AddChild(shop);
		Current = shop;
		return shop;
	}

	public override void _Ready()
	{
		var cache = new ConfigFile();
		if (cache.Load(CachePath) == Error.Ok)
			AdsRemoved = cache.GetValue("purchases", RemoveAds, false).AsBool();

		var args = OS.GetCmdlineUserArgs();
		if (args.Contains("--shop") || args.Contains("--shop=bought"))
		{
			// A pretend store for desktop checks; it never touches the real purchase record.
			_fake = true;
			AdsRemoved = args.Contains("--shop=bought");
			_prices[RemoveAds] = "$0.99";
			_prices[Tips[0]] = "$0.99";
			_prices[Tips[1]] = "$2.99";
			_prices[Tips[2]] = "$4.99";
			CallDeferred(MethodName.NotifyChanged);
			return;
		}
		if (!Engine.HasSingleton(PluginName))
			return;
		_billing = Engine.GetSingleton(PluginName);
		_billing.Call("initPlugin");
		_billing.Connect("connected", Callable.From(OnConnected));
		_billing.Connect("connect_error", Callable.From<int, string>((code, message) => BlackBox.Write($"shop: can't connect ({code}) {message}")));
		_billing.Connect("query_product_details_response", Callable.From<GodotDictionary>(OnProductDetails));
		_billing.Connect("query_purchases_response", Callable.From<GodotDictionary>(OnPurchases));
		_billing.Connect("on_purchase_updated", Callable.From<GodotDictionary>(OnPurchases));
		_billing.Call("startConnection");
	}

	/// <summary>Starts Google Play's purchase screen for a product.</summary>
	public void Buy(string product)
	{
		if (_fake)
		{
			Grant(product);
			return;
		}
		if (_billing == null || !_prices.ContainsKey(product))
			return;
		BlackBox.Write($"shop: buying {product}");
		var result = _billing.Call("purchase", product, "", "", false).AsGodotDictionary();
		if (result.TryGetValue("response_code", out var code) && code.AsInt32() != Ok)
			Message?.Invoke("Google Play couldn't start the purchase. Please try again later.");
	}

	private void NotifyChanged() => Changed?.Invoke();

	private void OnConnected()
	{
		BlackBox.Write("shop: connected to Google Play");
		var products = new[] { RemoveAds }.Concat(Tips).ToArray();
		_billing!.Call("queryProductDetails", products, "inapp");
		// Restores Remove ads on a new phone or after a reinstall, and finishes anything that
		// completed while the game was closed.
		_billing.Call("queryPurchases", "inapp", false);
	}

	private void OnProductDetails(GodotDictionary response)
	{
		if (response["response_code"].AsInt32() != Ok)
			return;
		foreach (var item in response["product_details"].AsGodotArray())
		{
			var details = item.AsGodotDictionary();
			string id = details["product_id"].AsString();
			if (details.TryGetValue("one_time_purchase_offer_details_list", out var offers) && offers.VariantType == Variant.Type.Array
				&& offers.AsGodotArray().FirstOrDefault() is { VariantType: Variant.Type.Dictionary } offer)
				_prices[id] = offer.AsGodotDictionary()["formatted_price"].AsString();
		}
		BlackBox.Write($"shop: {_prices.Count} products for sale");
		Changed?.Invoke();
	}

	/// <summary>Both the purchases Google Play already knows of and new ones arrive here.</summary>
	private void OnPurchases(GodotDictionary response)
	{
		int code = response["response_code"].AsInt32();
		if (code != Ok)
		{
			// 1 is the player backing out of the purchase screen: nothing to say.
			if (code != 1)
				BlackBox.Write($"shop: purchase response {code}");
			return;
		}
		foreach (var item in response["purchases"].AsGodotArray())
		{
			var purchase = item.AsGodotDictionary();
			int state = purchase["purchase_state"].AsInt32();
			string token = purchase["purchase_token"].AsString();
			foreach (var id in purchase["product_ids"].AsStringArray())
			{
				if (state == Pending)
				{
					Message?.Invoke("Thank you! Your purchase is waiting for Google Play to finish.");
					continue;
				}
				if (state != Purchased)
					continue;
				if (id == RemoveAds)
				{
					// A one-time purchase must be acknowledged within three days, or Google refunds it.
					if (!purchase["is_acknowledged"].AsBool())
						_billing!.Call("acknowledgePurchase", token);
					Grant(id);
				}
				else if (Tips.Contains(id))
				{
					// Tips are consumed straight away, so they can be given again.
					_billing!.Call("consumePurchase", token);
					Grant(id);
				}
			}
		}
	}

	private void Grant(string product)
	{
		if (product == RemoveAds)
		{
			if (!AdsRemoved)
				Message?.Invoke("Thank you! The ads are gone for good.");
			AdsRemoved = true;
			if (!_fake)
			{
				var cache = new ConfigFile();
				cache.Load(CachePath);
				cache.SetValue("purchases", RemoveAds, true);
				cache.Save(CachePath);
			}
			BlackBox.Write("shop: ads removed");
		}
		else
		{
			Message?.Invoke("Thank you so much for your support!");
			BlackBox.Write($"shop: tip {product}");
		}
		Changed?.Invoke();
	}
}
