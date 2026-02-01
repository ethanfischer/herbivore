using Godot;
using Herbivore.Data;

namespace Herbivore.Traversal;

public partial class PackMember : CharacterBody2D
{
	private enum WanderState { Idle, Walking, Paused }

	[Export]
	public DotType Type { get; set; } = DotType.Herbivore;

	[Export]
	public float FollowSpeed { get; set; } = 180.0f;

	[Export]
	public float FollowDistance { get; set; } = 25.0f;

	[Export]
	public float WanderSpeed { get; set; } = 20.0f;

	[Export]
	public float WanderRadius { get; set; } = 80.0f;

	[Export]
	public float PauseTimeMin { get; set; } = 1.0f;

	[Export]
	public float PauseTimeMax { get; set; } = 3.0f;

	private static Texture2D? _playerTexture;
	private static Texture2D? _enemyTexture;

	private Node2D? _leader;
	private Node2D? _player;
	private int _formationIndex = -1;
	private bool _isTested;
	private bool _isRecruited;
	private Sprite2D _sprite = null!;

	// Wandering state
	private Vector2 _homePosition;
	private Vector2 _wanderTarget;
	private WanderState _wanderState = WanderState.Idle;
	private float _pauseTimer;
	private bool _canWander = true;
	private RandomNumberGenerator _random = new();

	public bool IsTested => _isTested;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");

		// Load textures once (static cache)
		_playerTexture ??= GD.Load<Texture2D>("res://Assets/Graphics/player.png");
		_enemyTexture ??= GD.Load<Texture2D>("res://Assets/Graphics/enemy.png");

		_random.Randomize();
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
		if (_isRecruited && _player != null)
		{
			// Formation-based movement
			var targetPos = GetFormationPosition();
			var direction = targetPos - GlobalPosition;
			var distance = direction.Length();

			// Face toward the player
			var toPlayer = _player.GlobalPosition - GlobalPosition;
			if (toPlayer != Vector2.Zero)
			{
				_sprite.Rotation = toPlayer.Angle();
			}

			if (distance > 5f)
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
		else if (_leader != null)
		{
			// Original chain-following for non-recruited members
			var direction = _leader.GlobalPosition - GlobalPosition;
			var distance = direction.Length();

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
		else if (_canWander && !_isTested)
		{
			// Idle wandering behavior
			ProcessWander((float)delta);
		}
	}

	private void ProcessWander(float delta)
	{
		switch (_wanderState)
		{
			case WanderState.Idle:
				// Pick a new target and start walking
				PickNewWanderTarget();
				_wanderState = WanderState.Walking;
				break;

			case WanderState.Walking:
				var direction = _wanderTarget - GlobalPosition;
				var distance = direction.Length();

				if (distance > 5f)
				{
					// Move toward target
					Velocity = direction.Normalized() * WanderSpeed;
					MoveAndSlide();

					// Face movement direction
					if (direction != Vector2.Zero)
					{
						_sprite.Rotation = direction.Angle();
					}
				}
				else
				{
					// Arrived at target, start pause
					Velocity = Vector2.Zero;
					_pauseTimer = _random.RandfRange(PauseTimeMin, PauseTimeMax);
					_wanderState = WanderState.Paused;
				}
				break;

			case WanderState.Paused:
				_pauseTimer -= delta;
				if (_pauseTimer <= 0)
				{
					_wanderState = WanderState.Idle;
				}
				break;
		}
	}

	private void PickNewWanderTarget()
	{
		// Pick a random point within wander radius of home position
		var angle = _random.RandfRange(0, Mathf.Tau);
		var distance = _random.RandfRange(0, WanderRadius);
		_wanderTarget = _homePosition + new Vector2(
			Mathf.Cos(angle) * distance,
			Mathf.Sin(angle) * distance
		);
	}

	private Vector2 GetFormationPosition()
	{
		if (_player == null || _player is not PlayerDot playerDot) return GlobalPosition;

		// Triangle formation: row 0 has 2, row 1 has 3, row 2 has 4, etc.
		int row = 0;
		int indexInRow = _formationIndex;
		int rowSize = 2;

		while (indexInRow >= rowSize)
		{
			indexInRow -= rowSize;
			row++;
			rowSize = row + 2;
		}

		float spacing = 35f;
		float rowDistance = 40f;

		// Calculate horizontal offset for this position in the row
		float rowWidth = (rowSize - 1) * spacing;
		float xOffset = -rowWidth / 2 + indexInRow * spacing;
		float yOffset = rowDistance * (row + 1);

		// Get player's facing direction (persists when stopped)
		Vector2 facing = playerDot.FacingDirection;

		// Calculate perpendicular vector for horizontal spread
		Vector2 perpendicular = new Vector2(-facing.Y, facing.X);

		// Position behind player relative to movement direction
		return _player.GlobalPosition - facing * yOffset + perpendicular * xOffset;
	}

	public void SetLeader(Node2D leader)
	{
		_leader = leader;
	}

	public void SetFormation(Node2D player, int formationIndex)
	{
		_player = player;
		_formationIndex = formationIndex;
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

	public void SetHomePosition(Vector2 homePosition)
	{
		_homePosition = homePosition;
		_wanderTarget = homePosition;
	}

	public void SetWandering(bool canWander)
	{
		_canWander = canWander;
		if (!canWander)
		{
			Velocity = Vector2.Zero;
		}
	}
}
