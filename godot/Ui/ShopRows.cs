using System;
using System.Linq;
using Godot;
using Renegade.Store;

namespace Renegade.Ui;

/// <summary>
/// The "Support the game" section at the foot of Settings: Remove all ads (once, at its Google
/// Play price) and a row of tips. Hidden until Google Play has the products, and worded plainly:
/// the games are for children too, so no pressure, real prices, and a note to ask a grown-up.
/// </summary>
public static class ShopRows
{
	public static void Add(SettingsPanel panel, Node owner, Action<string> message)
	{
		if (Shop.Current is not { } shop)
			return;
		var heading = panel.AddHeading("Support the game", footer: true);
		var removeAds = panel.AddActions(true, ("Remove all ads", true, () => shop.Buy(Shop.RemoveAds)))[0];
		var tipNote = panel.AddNote("Enjoying it? You can leave a tip too.", footer: true);
		var tips = panel.AddActions(true, Shop.Tips.Select(id => ("Tip", false, (Action)(() => shop.Buy(id)))).ToArray());
		var note = panel.AddNote("Purchases go through Google Play. Ask a grown-up first.", footer: true);

		void Refresh()
		{
			bool open = shop.Open;
			heading.Visible = open;
			note.Visible = open;
			string? price = shop.Price(Shop.RemoveAds);
			removeAds.GetParent<Control>().Visible = open && (price != null || shop.AdsRemoved);
			removeAds.Text = shop.AdsRemoved ? "Ads removed. Thank you!" : $"Remove all ads   {price}";
			removeAds.Disabled = shop.AdsRemoved;
			bool anyTip = false;
			for (int i = 0; i < tips.Length; i++)
			{
				string? tip = shop.Price(Shop.Tips[i]);
				tips[i].Visible = tip != null;
				tips[i].Text = $"Tip {tip}";
				anyTip |= tip != null;
			}
			tips[0].GetParent<Control>().Visible = open && anyTip;
			tipNote.Visible = open && anyTip;
		}

		shop.Changed += Refresh;
		shop.Message += message;
		owner.TreeExiting += () =>
		{
			shop.Changed -= Refresh;
			shop.Message -= message;
		};
		Refresh();
	}

	/// <summary>A button label without its "(ad)" once the ads are gone: "Undo (ad)" becomes "Undo".</summary>
	public static string AdText(string text) =>
		Shop.Current?.AdsRemoved == true
			? System.Text.RegularExpressions.Regex.Replace(text, @"\s*\((?:\d+ )?ads?\)", "")
			: text;
}
