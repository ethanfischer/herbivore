using Godot;
using System.Collections.Generic;
using Herbivore.Data;
using Herbivore.Autoloads;

namespace Herbivore.Traversal;

public partial class NPCPack : Node2D
{
	[Signal]
	public delegate void PlayerApproachedEventHandler(NPCPack pack);

	[Export]
	public PackedScene? PackMemberScene { get; set; }

	[Export]
	public int MinMembers { get; set; } = 2;

	[Export]
	public int MaxMembers { get; set; } = 6;

	[Export]
	public float SpawnRadius { get; set; } = 120.0f;

	private readonly List<PackMember> _members = new();
	private Area2D _detectionArea = null!;
	private Area2D _awarenessArea = null!;
	private bool _isTested;
	private PlayerDot? _trackedPlayer; // Player being tracked while in awareness range
	private bool _isFriendly; // True if majority herbivore

	public bool IsTested => _isTested;
	public bool IsFriendly => _isFriendly;
	public int MemberCount => _members.Count;
	public IReadOnlyList<PackMember> Members => _members;

	private const float AwarenessRadius = 400.0f;

	public override void _Ready()
	{
		AddToGroup("npc_pack");

		_detectionArea = GetNode<Area2D>("DetectionArea");
		_detectionArea.AddToGroup("npc_pack");
		_detectionArea.BodyEntered += OnBodyEntered;

		// Create awareness area (larger, for turning to face player)
		_awarenessArea = new Area2D();
		_awarenessArea.CollisionLayer = 0; // Don't need to be detected
		_awarenessArea.CollisionMask = 1;  // Detect player (layer 1)
		_awarenessArea.Monitorable = false;
		var awarenessShape = new CollisionShape2D();
		var circleShape = new CircleShape2D();
		circleShape.Radius = AwarenessRadius;
		awarenessShape.Shape = circleShape;
		_awarenessArea.AddChild(awarenessShape);
		_awarenessArea.BodyEntered += OnAwarenessEntered;
		_awarenessArea.BodyExited += OnAwarenessExited;
		AddChild(_awarenessArea);

		SpawnMembers();
	}

	private const float MemberSpacing = 60.0f;
	private const float MinDistance = 40.0f;
	private const int MaxSpawnAttempts = 30;

	private void SpawnMembers()
	{
		if (PackMemberScene == null)
		{
			GD.PrintErr("NPCPack: PackMemberScene not set!");
			return;
		}

		var random = new RandomNumberGenerator();
		random.Randomize();

		int count = random.RandiRange(MinMembers, MaxMembers);
		int herbivoreCount = 0;
		var positions = new List<Vector2>();

		// First member at center
		positions.Add(Vector2.Zero);

		// Each subsequent member spawns adjacent to a random existing member
		for (int i = 1; i < count; i++)
		{
			Vector2 newPosition = Vector2.Zero;
			bool foundValidPosition = false;

			for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
			{
				// Pick a random existing member to spawn next to
				var basePosition = positions[random.RandiRange(0, positions.Count - 1)];

				// Random direction outward from that member
				var angle = random.RandfRange(0, Mathf.Tau);
				newPosition = basePosition + new Vector2(
					Mathf.Cos(angle) * MemberSpacing,
					Mathf.Sin(angle) * MemberSpacing
				);

				// Check if this position overlaps with any existing position
				if (!OverlapsAny(newPosition, positions))
				{
					foundValidPosition = true;
					break;
				}
			}

			if (foundValidPosition)
			{
				positions.Add(newPosition);
			}
		}

		// Calculate center of all positions
		var center = Vector2.Zero;
		foreach (var pos in positions)
			center += pos;
		center /= positions.Count;

		// Create members at calculated positions (may be fewer than count if overlaps couldn't be resolved)
		for (int i = 0; i < positions.Count; i++)
		{
			var member = PackMemberScene.Instantiate<PackMember>();
			member.Position = positions[i];

			// Random type
			var type = random.Randf() > 0.5f ? DotType.Herbivore : DotType.Carnivore;
			member.SetType(type);

			if (type == DotType.Herbivore)
				herbivoreCount++;

			AddChild(member);
			_members.Add(member);

			// Face towards center of group
			var directionToCenter = center - positions[i];
			member.SetFacingDirection(directionToCenter);
		}

		// Majority determines if friendly
		_isFriendly = herbivoreCount > count / 2;

		// Center detection area on the group and resize to cover all members
		float maxDistance = 0;
		foreach (var pos in positions)
		{
			var dist = pos.DistanceTo(center);
			if (dist > maxDistance)
				maxDistance = dist;
		}

		_detectionArea.Position = center;
		_awarenessArea.Position = center;
		var collisionShape = _detectionArea.GetNode<CollisionShape2D>("CollisionShape2D");
		if (collisionShape.Shape is CircleShape2D circleShape)
		{
			circleShape.Radius = maxDistance + 50.0f; // Add padding for approach distance
		}
	}

	private bool OverlapsAny(Vector2 position, List<Vector2> existingPositions)
	{
		foreach (var existing in existingPositions)
		{
			if (position.DistanceTo(existing) < MinDistance)
				return true;
		}
		return false;
	}

	public override void _Process(double delta)
	{
		if (_isTested || _trackedPlayer == null) return;

		// Continuously rotate all members to face the player
		foreach (var member in _members)
		{
			var directionToPlayer = _trackedPlayer.GlobalPosition - member.GlobalPosition;
			member.SetFacingDirection(directionToPlayer);
		}
	}

	private void OnAwarenessEntered(Node2D body)
	{
		if (_isTested) return;

		if (body is PlayerDot player)
		{
			_trackedPlayer = player;
		}
	}

	private void OnAwarenessExited(Node2D body)
	{
		if (body is PlayerDot)
		{
			_trackedPlayer = null;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_isTested) return;

		if (body is PlayerDot)
		{
			GD.Print($"Player approached NPC pack (Friendly: {_isFriendly})");
			EmitSignal(SignalName.PlayerApproached, this);
			GameManager.Instance?.ChangeState(GameState.Testing);
		}
	}

	public void MarkTested()
	{
		_isTested = true;

		// Darken all members to show they've been tested
		foreach (var member in _members)
		{
			member.Modulate = new Color(0.25f, 0.25f, 0.25f);
		}
	}

	public void TransferOneMemberToPlayer(Node2D playerPackContainer, Node2D lastLeader)
	{
		if (_members.Count == 0) return;

		// Take one member
		var member = _members[0];
		_members.RemoveAt(0);

		// Remember global position before reparenting
		var globalPos = member.GlobalPosition;

		// Reparent to player pack container
		member.GetParent().RemoveChild(member);
		playerPackContainer.AddChild(member);

		// Restore global position so they start from where the pack was
		member.GlobalPosition = globalPos;

		// Set up following chain
		member.SetLeader(lastLeader);

		// Mark as recruited (turns green)
		member.MarkRecruited();

		// Add to GameManager
		GameManager.Instance?.AddToPlayerPack(member);

		// Pack stays but is now tested (can't be approached again)
	}

	public void RemovePackFromGame()
	{
		foreach (var member in _members)
		{
			member.QueueFree();
		}
		_members.Clear();
		QueueFree();
	}
}
