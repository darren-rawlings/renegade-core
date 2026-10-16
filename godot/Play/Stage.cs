using Godot;
using Renegade.Looks;

namespace Renegade.Play;

/// <summary>
/// The shared setting every game starts from: warm key and fill lights, a dark background, a sky
/// that's only reflected (without one, steel and silver render nearly black), and a plank table
/// big enough that its edge is never seen.
/// </summary>
public static class Stage
{
	/// <param name="tableY">Height of the tabletop.</param>
	public static StaticBody3D Build(Node3D parent, float tableY, Color background)
	{
		parent.AddChild(new DirectionalLight3D
		{
			ShadowEnabled = true,
			LightEnergy = 1.15f,
			LightColor = new Color("fff1dc"),
			RotationDegrees = new Vector3(-60, 35, 0),
		});
		parent.AddChild(new OmniLight3D { Position = new Vector3(-4, 4, 4), OmniRange = 16, LightEnergy = 0.5f, LightColor = new Color("ffcf9a") });
		parent.AddChild(new WorldEnvironment
		{
			Environment = new Environment
			{
				BackgroundMode = Environment.BGMode.Color,
				BackgroundColor = background,
				AmbientLightSource = Environment.AmbientSource.Color,
				AmbientLightColor = new Color("6e5444"),
				AmbientLightEnergy = 0.9f,
				ReflectedLightSource = Environment.ReflectionSource.Sky,
				Sky = new Sky
				{
					SkyMaterial = new ProceduralSkyMaterial
					{
						SkyTopColor = new Color("8a6a52"),
						SkyHorizonColor = new Color("e8c9a0"),
						GroundBottomColor = new Color("2a1a10"),
						GroundHorizonColor = new Color("6e4c34"),
						SunAngleMax = 30,
					},
				},
			},
		});

		var table = new StaticBody3D { Position = new Vector3(0, tableY, 0) };
		table.AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(160, 160) }, MaterialOverride = Materials.For(Styles.BoardLook(Style.Table)) });
		table.AddChild(new CollisionShape3D { Shape = new WorldBoundaryShape3D() });
		parent.AddChild(table);
		return table;
	}

	/// <summary>
	/// Phones compile each shader the first time it's drawn, which stalls that frame. Drawing every
	/// material once at startup, hidden under the table, pays that cost up front.
	/// </summary>
	public static void WarmUp(Node3D parent, float tableY, params Material[] extra)
	{
		var warm = new Node3D { Position = new Vector3(0, tableY - 1f, 0) };
		parent.AddChild(warm);
		var sphere = new SphereMesh { Radius = 0.1f, Height = 0.2f };
		foreach (var style in Styles.Choices)
		{
			warm.AddChild(new MeshInstance3D { Mesh = sphere, MaterialOverride = Materials.For(Styles.BoardLook(style)) });
			warm.AddChild(new MeshInstance3D { Mesh = sphere, MaterialOverride = Materials.For(Styles.Pieces(style).One) });
		}
		foreach (var material in extra)
			warm.AddChild(new MeshInstance3D { Mesh = sphere, MaterialOverride = material });
		warm.AddChild(new Sparkle());
		warm.AddChild(new MeshInstance3D { Mesh = sphere, MaterialOverride = Materials.For(Styles.Pieces(Style.Plastic).One), MaterialOverlay = Flames.Overlay() });
		warm.AddChild(new Flames());
		parent.GetTree().CreateTimer(0.4).Timeout += warm.QueueFree;
	}
}
