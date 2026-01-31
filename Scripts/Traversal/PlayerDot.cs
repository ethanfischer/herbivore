using Godot;

namespace Herbivore.Traversal;

public partial class PlayerDot : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 200.0f;

    private Area2D _approachArea = null!;

    public override void _Ready()
    {
        _approachArea = GetNode<Area2D>("ApproachArea");
        _approachArea.AreaEntered += OnApproachAreaEntered;
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
