using Godot;
using Renegade.Core.Synth;

namespace Renegade.Looks;

/// <summary>
/// The studio's material families. Every game offers the same five in its Style menu, so a player
/// who likes steel in Counter Fire finds steel in Mancala, looking and sounding the same.
/// </summary>
public enum Style
{
	Plastic,
	Wood,
	Metal,
	Glass,
	Marble,

	/// <summary>The tabletop under the board. Not selectable in the menu.</summary>
	Table,
}

/// <summary>Shader settings for one surface. <see cref="Pattern"/> ids match shaders/surface.gdshader.</summary>
public sealed record Look(
	int Pattern,
	Color A,
	Color B,
	float Roughness,
	float Metallic = 0f,
	float Scale = 1f,
	float Opacity = 1f)
{
	public const int PlasticPattern = 0;
	public const int WoodPattern = 1;
	public const int MetalPattern = 2;
	public const int MarblePattern = 3;
	public const int GlassPattern = 4;
	public const int PlankPattern = 5;

	public bool IsGlass => Pattern == GlassPattern;
}

/// <summary>A material's two playing colours (one per side), their names, and matching UI colours.</summary>
public sealed record PieceSet(string Name, string NameOne, string NameTwo, Look One, Look Two, Color UiOne, Color UiTwo)
{
	public Look LookOf(bool first) => first ? One : Two;
	public string NameOf(bool first) => first ? NameOne : NameTwo;
	public Color UiColorOf(bool first) => first ? UiOne : UiTwo;
}

/// <summary>
/// Every tunable number for the materials, shared by all games. The sounds were tuned on a real
/// phone speaker (Galaxy Z Fold): thin, high or pure tones vanish there, so each recipe has a
/// sharp strike and enough body to carry.
/// </summary>
public static class Styles
{
	public static readonly Style[] Choices = { Style.Plastic, Style.Wood, Style.Metal, Style.Glass, Style.Marble };

	public static string BoardName(Style style) => style switch
	{
		Style.Plastic => "Classic",
		Style.Wood => "Oak",
		Style.Metal => "Steel",
		Style.Glass => "Glass",
		Style.Marble => "Marble",
		_ => "Table",
	};

	public static Look BoardLook(Style style) => style switch
	{
		Style.Wood => new(Look.WoodPattern, new("a4693c"), new("845030"), 0.5f, Scale: 1.1f),
		Style.Metal => new(Look.MetalPattern, new("c3c9d2"), new("7d848f"), 0.3f, Metallic: 1f),
		Style.Glass => new(Look.GlassPattern, new("9fd4ff"), new("5aa0e0"), 0.05f, Opacity: 0.35f),
		Style.Marble => new(Look.MarblePattern, new("b9b3a8"), new("5e5a54"), 0.2f, Scale: 0.55f),
		Style.Table => new(Look.PlankPattern, new("4a2e1c"), new("2c1a10"), 0.75f, Scale: 0.6f),
		_ => new(Look.PlasticPattern, new("2f5fe8"), new("1c3a9c"), 0.3f),
	};

	/// <summary>Two-colour piece sets, for games where each side has its own colour.</summary>
	public static PieceSet Pieces(Style style) => style switch
	{
		Style.Wood => new("Wood", "Walnut", "Maple",
			new(Look.WoodPattern, new("6b4426"), new("3b2414"), 0.55f, Scale: 3f),
			new(Look.WoodPattern, new("e8c891"), new("c49a5e"), 0.5f, Scale: 3f),
			new("c08850"), new("e8c891")),
		Style.Metal => new("Metal", "Copper", "Silver",
			new(Look.MetalPattern, new("d08850"), new("8f4a24"), 0.32f, Metallic: 1f),
			new(Look.MetalPattern, new("e3e7ec"), new("9aa1ab"), 0.24f, Metallic: 1f),
			new("e09060"), new("e3e7ec")),
		Style.Glass => new("Glass", "Ruby", "Sapphire",
			new(Look.GlassPattern, new("e11d48"), new("fda4af"), 0.04f, Opacity: 0.62f),
			new(Look.GlassPattern, new("2563eb"), new("93c5fd"), 0.04f, Opacity: 0.62f),
			new("ff4d7a"), new("6b9bff")),
		Style.Marble => new("Marble", "Onyx", "Ivory",
			new(Look.MarblePattern, new("202026"), new("8a8a96"), 0.15f, Scale: 4f),
			new(Look.MarblePattern, new("f3eee2"), new("b9ae96"), 0.15f, Scale: 4f),
			new("a6abbf"), new("f3eee2")),
		_ => new("Plastic", "Red", "Yellow",
			new(Look.PlasticPattern, new("ef4444"), new("b91c1c"), 0.28f),
			new(Look.PlasticPattern, new("facc15"), new("ca8a04"), 0.28f),
			new("ef4444"), new("facc15")),
	};

	/// <summary>One colour per material, for games where every piece is alike (Mancala's marbles).</summary>
	public static (string Name, Look Look) Solo(Style style) => style switch
	{
		Style.Wood => ("Walnut", Pieces(style).One),
		Style.Metal => ("Silver", Pieces(style).Two),
		Style.Glass => ("Sapphire", Pieces(style).Two),
		Style.Marble => ("Ivory", Pieces(style).Two),
		_ => ("Red", Pieces(style).One),
	};

	public static ImpactRecipe Impact(Style style) => style switch
	{
		Style.Wood => new(750, new[] { 1.0, 2.57, 4.2, 6.1 }, new[] { 1.0, 0.6, 0.3, 0.15 }, 0.045, 0.7, 0.006, 0.35, 0.3),
		Style.Metal => new(2100, new[] { 1.0, 2.756, 5.404, 8.933 }, new[] { 1.0, 0.7, 0.5, 0.3 }, 0.45, 0.3, 0.002, 0.7, 0.9),
		// A pure 3.3 kHz ring vanished on the phone speaker; a lower ring with a sharp clink carries.
		Style.Glass => new(2300, new[] { 1.0, 2.32, 4.25, 6.63 }, new[] { 1.0, 0.6, 0.45, 0.3 }, 0.18, 0.9, 0.004, 0.8, 0.6),
		Style.Marble => new(1400, new[] { 1.0, 1.83, 2.9, 4.4 }, new[] { 1.0, 0.7, 0.4, 0.2 }, 0.05, 0.9, 0.005, 0.55, 0.3),
		Style.Table => new(420, new[] { 1.0, 2.4, 3.9 }, new[] { 1.0, 0.4, 0.2 }, 0.03, 0.8, 0.008, 0.2, 0.25),
		// Plastic rang for only ~35 ms and got lost on the phone; 50 ms carries.
		_ => new(1900, new[] { 1.0, 2.2, 3.7 }, new[] { 1.0, 0.5, 0.25 }, 0.05, 0.5, 0.004, 0.5, 0.3),
	};

	/// <summary>Long-ringing materials are quieter per hit so a cascade doesn't get harsh.</summary>
	public static float GainDb(Style style) => style switch
	{
		Style.Metal => -5f,
		Style.Glass => -2f,
		Style.Table => -2f,
		_ => 0f,
	};

	public static float Bounce(Style style) => style switch
	{
		Style.Metal => 0.3f,
		Style.Glass => 0.3f,
		Style.Wood => 0.15f,
		Style.Marble => 0.15f,
		_ => 0.2f,
	};
}
