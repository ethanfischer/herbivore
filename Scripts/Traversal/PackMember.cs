using Godot;
using Herbivore.Data;

namespace Herbivore.Traversal;

public partial class PackMember : CharacterBody2D
{
	[Export]
	public DotType Type { get; set; } = DotType.Herbivore;

	[Export]
	public float FollowSpeed { get; set; } = 180.0f;

	[Export]
	public float FollowDistance { get; set; } = 25.0f;

	private static Texture2D? _playerTexture;
	private static Texture2D? _enemyTexture;

	private Node2D? _leader;
	private bool _isTested;
	private bool _isRecruited;
	private Sprite2D _sprite = null!;

	public bool IsTested => _isTested;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");

		// Load textures once (static cache)
		_playerTexture ??= GD.Load<Texture2D>("res://Assets/Graphics/player.png");
		_enemyTexture ??= GD.Load<Texture2D>("res://Assets/Graphics/enemy.png");

		UpdateSprite();
	}

	public override void _Draw()
	{
		// Draw shadow
		DrawEllipse(new Vector2(0, 5), new Vector2(25, 25), new Color(0, 0, 0, 0.5f));
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

	private void UpdateSprite()
	{
		_sprite.Texture = _isRecruited ? _playerTexture : _enemyTexture;
	}

	public void MarkRecruited()
	{
		_isRecruited = true;
		UpdateSprite();

		// Disable collision with player so recruited members don't block movement
		CollisionLayer = 0;
		CollisionMask = 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_leader == null) return;

		var direction = _leader.GlobalPosition - GlobalPosition;
		var distance = direction.Length();

		// Always face toward the leader when recruited
		if (_isRecruited && direction != Vector2.Zero)
		{
			_sprite.Rotation = direction.Angle();
		}

		if (distance > FollowDistance)
		{
			var velocity = direction.Normalized() * FollowSpeed;
			Velocity = velocity;
			MoveAndSlide();
		}
		else
		{
			Velocity = Vector2.Zero;
		}
	}

	public void SetLeader(Node2D leader)
	{
		_leader = leader;
	}

	public void MarkTested()
	{
		_isTested = true;
	}

	public void SetType(DotType type)
	{
		Type = type;
	}

	public void SetFacingDirection(Vector2 direction)
	{
		if (direction != Vector2.Zero)
		{
			_sprite.Rotation = direction.Angle();
		}
	}
}
