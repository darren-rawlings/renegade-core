using System.Collections.Generic;
using Godot;

namespace Renegade.Looks;

/// <summary>Turns <see cref="Look"/> records into shader materials, one cached per look and variant.</summary>
public static class Materials
{
	/// <summary>Seeded variants per look, so no two wooden pieces share grain.</summary>
	public const int Variants = 6;

	private static readonly Shader SurfaceShader = GD.Load<Shader>("res://addons/renegade_core/shaders/surface.gdshader");
	private static readonly Shader GlassShader = GD.Load<Shader>("res://addons/renegade_core/shaders/glass.gdshader");
	private static readonly Dictionary<(Look, bool, int), ShaderMaterial> Cache = new();

	/// <param name="isDisc">Lathed pieces (discs, lathed around local Y) get moulded rings and radial brushing.</param>
	public static ShaderMaterial For(Look look, bool isDisc = false, int variant = 0)
	{
		variant %= Variants;
		if (Cache.TryGetValue((look, isDisc, variant), out var cached))
			return cached;
		var material = new ShaderMaterial { Shader = look.IsGlass ? GlassShader : SurfaceShader };
		material.SetShaderParameter("color_a", look.A);
		material.SetShaderParameter("color_b", look.B);
		material.SetShaderParameter("roughness_base", look.Roughness);
		material.SetShaderParameter("pattern_scale", look.Scale);
		material.SetShaderParameter("is_disc", isDisc);
		material.SetShaderParameter("seed", new Vector3(13.1f, 7.7f, 3.3f) * variant);
		if (look.IsGlass)
		{
			material.SetShaderParameter("opacity", look.Opacity);
		}
		else
		{
			material.SetShaderParameter("pattern", look.Pattern);
			material.SetShaderParameter("metallic_base", look.Metallic);
		}
		Cache[(look, isDisc, variant)] = material;
		return material;
	}
}
