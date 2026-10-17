using System;
using Godot;

namespace Renegade.Play;

/// <summary>
/// Turns touches into taps and board spins, as in Counter Fire: a press that moves more than a
/// few pixels sideways spins <see cref="Target"/> around Y and keeps turning after a flick; a press
/// that doesn't move is a tap. Feed it input from _UnhandledInput and call <see cref="Process"/>.
/// </summary>
public sealed class SpinControl
{
	private const float SpinPerPixel = 0.008f;
	private const float SpinDamping = 1.6f;
	private const float DragThreshold = 14f;

	private bool _pressing;
	private bool _dragging;
	private Vector2 _pressPosition;
	private float _velocity;
	private ulong _lastDragUsec;

	public SpinControl(Node3D target, Func<bool> canSpin)
	{
		Target = target;
		CanSpin = canSpin;
		Home = target.Rotation.Y;
	}

	public Node3D Target { get; }

	/// <summary>Spinning is allowed only while this says so (for example, when no piece is moving).</summary>
	public Func<bool> CanSpin { get; }

	/// <summary>The board's starting angle, where Freeze the board puts it back.</summary>
	public float Home { get; set; }

	/// <summary>
	/// While this says so (Freeze the board is on, and nothing is moving), the board eases back to
	/// <see cref="Home"/> and stays there.
	/// </summary>
	public Func<bool>? Frozen { get; set; }

	/// <summary>Raised with the screen position of a press that didn't turn into a drag.</summary>
	public event Action<Vector2>? Tapped;

	/// <summary>
	/// Lets a game take a press for itself (dragging a piece into place): return true and the
	/// press goes to <see cref="Dragged"/> and <see cref="Released"/> instead of spinning.
	/// </summary>
	public Func<Vector2, bool>? TryGrab { get; set; }

	public event Action<Vector2>? Dragged;

	/// <summary>Raised when a grabbed press ends, with where, and whether it moved (false = it was a tap).</summary>
	public event Action<Vector2, bool>? Released;

	private bool _grabbed;

	public void HandleInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
				_grabbed = TryGrab?.Invoke(press.Position) == true;
				_pressing = true;
				_dragging = false;
				_pressPosition = press.Position;
				_velocity = 0f;
				break;

			case InputEventMouseButton { ButtonIndex: MouseButton.Left } release:
				if (_grabbed)
				{
					_grabbed = false;
					_pressing = false;
					Released?.Invoke(release.Position, release.Position.DistanceTo(_pressPosition) > DragThreshold);
					break;
				}
				if (_pressing && !_dragging)
					Tapped?.Invoke(release.Position);
				_pressing = false;
				_dragging = false;
				break;

			case InputEventMouseMotion motion when _grabbed:
				Dragged?.Invoke(motion.Position);
				break;

			case InputEventMouseMotion motion:
				if (_pressing && !_dragging && motion.Position.DistanceTo(_pressPosition) > DragThreshold && CanSpin())
				{
					_dragging = true;
					_lastDragUsec = Time.GetTicksUsec();
				}
				if (_dragging)
					Drag(motion.Relative.X);
				break;
		}
	}

	public void Stop() => _velocity = 0f;

	public void Process(float dt)
	{
		if (Frozen?.Invoke() == true)
		{
			_velocity = 0f;
			float off = Mathf.AngleDifference(Target.Rotation.Y, Home);
			if (Mathf.Abs(off) > 0.0005f)
				Target.RotateY(Mathf.Abs(off) < 0.002f ? off : off * (1f - Mathf.Exp(-9f * dt)));
			return;
		}
		if (_dragging || _velocity == 0f)
			return;
		if (!CanSpin() || Mathf.Abs(_velocity) < 0.02f)
		{
			_velocity = 0f;
			return;
		}
		Target.RotateY(_velocity * dt);
		_velocity *= Mathf.Exp(-SpinDamping * dt);
	}

	private void Drag(float pixels)
	{
		if (!CanSpin())
			return;
		float angle = pixels * SpinPerPixel;
		Target.RotateY(angle);
		ulong now = Time.GetTicksUsec();
		float dt = Mathf.Max((now - _lastDragUsec) / 1_000_000f, 0.001f);
		_lastDragUsec = now;
		// Remember how fast the finger was moving, so a flick keeps the board turning.
		_velocity = Mathf.Lerp(_velocity, angle / dt, 0.5f);
	}
}
