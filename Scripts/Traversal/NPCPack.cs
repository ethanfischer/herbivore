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
    public float SpawnRadius { get; set; } = 30.0f;

    private readonly List<PackMember> _members = new();
    private Area2D _detectionArea = null!;
    private bool _isTested;
    private bool _isFriendly; // True if majority herbivore

    public bool IsTested => _isTested;
    public bool IsFriendly => _isFriendly;
    public int MemberCount => _members.Count;
    public IReadOnlyList<PackMember> Members => _members;

    public override void _Ready()
    {
        AddToGroup("npc_pack");

        _detectionArea = GetNode<Area2D>("DetectionArea");
        _detectionArea.AddToGroup("npc_pack");
        _detectionArea.BodyEntered += OnBodyEntered;

        SpawnMembers();
    }

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

        for (int i = 0; i < count; i++)
        {
            var member = PackMemberScene.Instantiate<PackMember>();

            // Random position within spawn radius
            var angle = random.RandfRange(0, Mathf.Tau);
            var distance = random.RandfRange(0, SpawnRadius);
            member.Position = new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );

            // Random type
            var type = random.Randf() > 0.5f ? DotType.Herbivore : DotType.Carnivore;
            member.SetType(type);

            if (type == DotType.Herbivore)
                herbivoreCount++;

            AddChild(member);
            _members.Add(member);
        }

        // Majority determines if friendly
        _isFriendly = herbivoreCount > count / 2;
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
