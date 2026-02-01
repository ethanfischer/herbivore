using Godot;

namespace Herbivore.Traversal;

public partial class PlayerDot : CharacterBody2D
{
	[Export]
	public float Speed { get; set; } = 200.0f;

	[Export]
	public Color DotColor { get; set; } = new Color(0.2f, 0.6f, 1.0f); // Blue

	[Export]
	public float DotRadius { get; set; } = 8.0f;

	private Area2D _approachArea = null!;
	private Sprite2D _sprite = null!;
	private AudioStreamPlayer _walkSound = null!;
	private Vector2 _lastFacingDirection = Vector2.Down;

	public Vector2 FacingDirection => _lastFacingDirection;

	public override void _Ready()
	{
		_approachArea = GetNode<Area2D>("ApproachArea");
		_approachArea.AreaEntered += OnApproachAreaEntered;
		_sprite = GetNode<Sprite2D>("Sprite2D");

		// Get walk sound from main scene
		_walkSound = GetTree().CurrentScene.GetNode<AudioStreamPlayer>("Sounds/SandWalk");
	}

	public override void _Draw()
	{
		// Draw shadow first (behind)
		DrawEllipse(new Vector2(0, 5), new Vector2(30, 30), new Color(0, 0, 0, 0.5f));
		// Draw player dot
		DrawCircle(Vector2.Zero, DotRadius, DotColor);
	}

	private void DrawEllipse(Vector2 center, Vector2 size, Color color)
	{
		var points = new Vector2[32];
		for (int i = 0; i < 32; i++)
		{
			float angle = i * Mathf.Tau / 32;
			points[i] = center + new Vector2(Mathf.Cos(angle) * size.X / 2, Mathf.Sin(angle) * size.Y / 2);
		}
		DrawColoredPolygon(points, color);
	}

	public override void _PhysicsProcess(double delta)
	{
		var velocity = Vector2.Zero;

		if (Input.IsActionPressed("ui_up") || Input.IsActionPressed("move_up"))
			velocity.Y -= 1;
		if (Input.IsActionPressed("ui_down") || Input.IsActionPressed("move_down"))
			velocity.Y += 1;
		if (Input.IsActionPressed("ui_left") || Input.IsActionPressed("move_left"))
			velocity.X -= 1;
		if (Input.IsActionPressed("ui_right") || Input.IsActionPressed("move_right"))
			velocity.X += 1;

		if (velocity != Vector2.Zero)
		{
			velocity = velocity.Normalized() * Speed;
			// Rotate sprite to face movement direction
			_sprite.Rotation = velocity.Angle();
			// Track facing direction
			_lastFacingDirection = velocity.Normalized();

			// Play walk sound if not already playing
			if (!_walkSound.Playing)
				_walkSound.Play();
		}
		else
		{
			// Stop walk sound when not moving
			_walkSound.Stop();
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void OnApproachAreaEntered(Area2D area)
	{
		// Will be used to trigger test mode when approaching NPC packs
		if (area.IsInGroup("npc_pack"))
		{
			GD.Print("Approached NPC pack!");
			// GameManager.Instance.StartTest(area.GetParent());
		}
	}
}
