using System.Collections.Generic;
using Godot;

namespace Renegade.Play;

/// <summary>
/// The studio's winner's fire, from Counter Fire: a flare as it catches, steady flames and embers,
/// and optionally a flickering light. Particles live in world space, so they rise straight up even
/// as the board spins. Use <see cref="SetAlight"/> to set a piece burning: its meshes get the
/// fire overlay (with a bright flash as they catch) and flames rise from it.
/// Phones manage roughly a dozen burning pieces; beyond that, give the rest the overlay only.
/// </summary>
public partial class Flames : Node3D
{
	private const float FlameLifetime = 0.9f;
	private const float EmberLifetime = 1.4f;

	private static readonly Shader FireShader = GD.Load<Shader>("res://addons/renegade_core/shaders/fire.gdshader");
	private static StandardMaterial3D? _material;
	private static readonly Dictionary<float, QuadMesh> FlameQuads = new();
	private static QuadMesh? _emberQuad;

	private readonly List<CpuParticles3D> _burning = new();
	private readonly CpuParticles3D _flare;
	private OmniLight3D? _light;
	private float _lightEnergy;
	private bool _out;

	/// <param name="radius">How wide the fire is at its base.</param>
	/// <param name="size">Flame size; 1 suits a checker-sized piece.</param>
	/// <param name="withLight">One light per scene is plenty; more cost a lighting pass each on phones.</param>
	public Flames(float radius = 0.3f, float size = 1f, bool withLight = false)
	{
		var flames = MakeFlames(radius, size);
		var embers = MakeEmbers(radius);
		_flare = MakeFlare(radius, size);
		AddChild(flames);
		AddChild(embers);
		AddChild(_flare);
		_burning.Add(flames);
		_burning.Add(embers);
		if (withLight)
		{
			_light = new OmniLight3D { Position = new Vector3(0, 0.5f, 0.4f), LightColor = new Color(1f, 0.55f, 0.2f), OmniRange = 5f, LightEnergy = 0f };
			AddChild(_light);
		}
	}

	/// <summary>
	/// Sets a piece alight: the fire overlay on its meshes (flashing bright as it catches) and flames
	/// rising from <paramref name="at"/> (in <paramref name="parent"/>'s space). Returns the flames.
	/// </summary>
	public static Flames SetAlight(Node3D parent, Vector3 at, IEnumerable<MeshInstance3D> meshes, float radius = 0.3f, float size = 1f, bool withLight = false, bool particles = true)
	{
		var overlay = Overlay();
		foreach (var mesh in meshes)
			mesh.MaterialOverlay = overlay;
		var tween = parent.CreateTween();
		tween.TweenMethod(Callable.From<float>(h => overlay.SetShaderParameter("heat", h)), 0f, 2.2f, 0.12f);
		tween.TweenMethod(Callable.From<float>(h => overlay.SetShaderParameter("heat", h)), 2.2f, 1f, 0.4f);
		var flames = new Flames(radius, size, withLight) { Position = at, Visible = particles };
		parent.AddChild(flames);
		return flames;
	}

	/// <summary>A new fire overlay material, so every burning piece can flare on its own.</summary>
	public static ShaderMaterial Overlay() => new() { Shader = FireShader };

	public override void _Ready()
	{
		_flare.Restart();
		if (_light != null)
			CreateTween().TweenMethod(Callable.From<float>(e => _lightEnergy = e), 0f, 2.2f, 0.25f);
	}

	public override void _Process(double delta)
	{
		if (_light == null)
			return;
		// Two out-of-step wobbles plus a little noise read as firelight rather than a pulse.
		float t = Time.GetTicksMsec() / 1000f;
		float flicker = 0.8f + 0.12f * Mathf.Sin(t * 13f) + 0.08f * Mathf.Sin(t * 29f + 1.3f) + (float)GD.RandRange(-0.05, 0.05);
		_light.LightEnergy = _lightEnergy * flicker;
	}

	/// <summary>Stops emitting, lets the last flames rise and fade, then removes itself.</summary>
	public void Extinguish()
	{
		if (_out || !IsInsideTree())
			return;
		_out = true;
		foreach (var emitter in _burning)
			emitter.Emitting = false;
		if (_light != null)
			CreateTween().TweenMethod(Callable.From<float>(e => _lightEnergy = e), _lightEnergy, 0f, 0.3f);
		GetTree().CreateTimer(EmberLifetime).Timeout += QueueFree;
	}

	private static StandardMaterial3D Material => _material ??= new StandardMaterial3D
	{
		ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
		Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		BlendMode = BaseMaterial3D.BlendModeEnum.Add,
		BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
		VertexColorUseAsAlbedo = true,
		AlbedoTexture = SoftDot(),
		DisableReceiveShadows = true,
	};

	/// <summary>Taller than wide, so each billboard reads as a tongue of flame rather than a puff.</summary>
	private static QuadMesh FlameQuad(float size)
	{
		if (!FlameQuads.TryGetValue(size, out var quad))
			FlameQuads[size] = quad = new QuadMesh { Size = new Vector2(0.45f, 0.7f) * size, Material = Material };
		return quad;
	}

	private static GradientTexture2D SoftDot()
	{
		var gradient = new Gradient();
		gradient.SetColor(0, Colors.White);
		gradient.SetColor(1, new Color(1, 1, 1, 0));
		return new GradientTexture2D { Gradient = gradient, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f), Width = 64, Height = 64 };
	}

	private static CpuParticles3D Base(int amount, float lifetime, QuadMesh mesh) => new()
	{
		Amount = amount,
		Lifetime = lifetime,
		Mesh = mesh,
		LocalCoords = false,
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		Direction = Vector3.Up,
	};

	/// <summary>The body of the fire: hot yellow at the base, cooling to red as it rises.</summary>
	private static CpuParticles3D MakeFlames(float radius, float size)
	{
		var flames = Base(24, FlameLifetime, FlameQuad(size));
		flames.EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere;
		flames.EmissionSphereRadius = radius;
		flames.Spread = 12f;
		flames.Gravity = new Vector3(0, 4.5f * size, 0);
		flames.InitialVelocityMin = 0.6f * size;
		flames.InitialVelocityMax = 1.4f * size;
		flames.ScaleAmountMin = 0.6f;
		flames.ScaleAmountMax = 1.1f;
		flames.ScaleAmountCurve = Shrink();
		// Blending is additive, so colours are kept dim: bright ones washed out to white on light boards.
		flames.ColorRamp = Ramp(
			(0f, new Color(1f, 0.7f, 0.2f, 0f)),
			(0.1f, new Color(1f, 0.6f, 0.15f, 0.7f)),
			(0.45f, new Color(0.95f, 0.3f, 0.03f, 0.55f)),
			(0.8f, new Color(0.6f, 0.08f, 0.01f, 0.3f)),
			(1f, new Color(0.2f, 0.02f, 0f, 0f)));
		return flames;
	}

	private static CpuParticles3D MakeEmbers(float radius)
	{
		_emberQuad ??= new QuadMesh { Size = new Vector2(0.09f, 0.09f), Material = Material };
		var embers = Base(6, EmberLifetime, _emberQuad);
		embers.EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere;
		embers.EmissionSphereRadius = radius * 0.8f;
		embers.Spread = 35f;
		embers.Gravity = new Vector3(0, 1.5f, 0);
		embers.InitialVelocityMin = 1f;
		embers.InitialVelocityMax = 2.2f;
		embers.ColorRamp = Ramp((0f, new Color(1f, 0.9f, 0.5f, 1f)), (0.6f, new Color(1f, 0.4f, 0.05f, 0.9f)), (1f, new Color(0.6f, 0.1f, 0f, 0f)));
		return embers;
	}

	/// <summary>The burst when it catches: one outward puff of flame.</summary>
	private static CpuParticles3D MakeFlare(float radius, float size)
	{
		var flare = Base(12, 0.45f, FlameQuad(size));
		flare.OneShot = true;
		flare.Emitting = false;
		flare.Explosiveness = 0.95f;
		flare.EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere;
		flare.EmissionSphereRadius = radius * 0.6f;
		flare.Spread = 180f;
		flare.Gravity = new Vector3(0, 2f, 0);
		flare.InitialVelocityMin = 1.5f * size;
		flare.InitialVelocityMax = 2.5f * size;
		flare.ScaleAmountMin = 0.9f;
		flare.ScaleAmountMax = 1.4f;
		flare.ScaleAmountCurve = Shrink();
		flare.ColorRamp = Ramp((0f, new Color(1f, 1f, 0.85f, 1f)), (0.35f, new Color(1f, 0.6f, 0.15f, 0.8f)), (1f, new Color(0.6f, 0.1f, 0.02f, 0f)));
		return flare;
	}

	private static Curve Shrink()
	{
		var curve = new Curve();
		curve.AddPoint(new Vector2(0f, 0.7f));
		curve.AddPoint(new Vector2(0.3f, 1f));
		curve.AddPoint(new Vector2(1f, 0.2f));
		return curve;
	}

	private static Gradient Ramp(params (float Offset, Color Color)[] stops) => new()
	{
		Offsets = System.Array.ConvertAll(stops, s => s.Offset),
		Colors = System.Array.ConvertAll(stops, s => s.Color),
	};
}
