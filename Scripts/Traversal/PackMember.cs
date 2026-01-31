using Godot;
using Herbivore.Data;

namespace Herbivore.Traversal;

public partial class PackMember : CharacterBody2D
{
    [Export]
    public DotType Type { get; set; } = DotType.Herbivore;

    [Export]
    public float DotRadius { get; set; } = 6.0f;

    [Export]
    public Color HerbivoreColor { get; set; } = new Color(0.2f, 0.8f, 0.3f); // Green

    [Export]
    public Color CarnivoreColor { get; set; } = new Color(0.9f, 0.2f, 0.2f); // Red

    [Export]
    public float FollowSpeed { get; set; } = 180.0f;

    [Export]
    public float FollowDistance { get; set; } = 25.0f;

    private Node2D? _leader;
    private bool _isTested;

    public bool IsTested => _isTested;

    public override void _Draw()
    {
        var color = Type == DotType.Herbivore ? HerbivoreColor : CarnivoreColor;
        DrawCircle(Vector2.Zero, DotRadius, color);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_leader == null) return;

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
        QueueRedraw();
    }
}
