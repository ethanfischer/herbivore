using Godot;

namespace Herbivore.Traversal;

public partial class CameraFollow : Camera2D
{
    [Export]
    public NodePath? TargetPath { get; set; }

    [Export]
    public float SmoothSpeed { get; set; } = 5.0f;

    private Node2D? _target;

    public override void _Ready()
    {
        if (TargetPath != null && !TargetPath.IsEmpty)
        {
            _target = GetNode<Node2D>(TargetPath);
        }
    }

    public override void _Process(double delta)
    {
        if (_target == null) return;

        GlobalPosition = GlobalPosition.Lerp(_target.GlobalPosition, (float)(SmoothSpeed * delta));
    }

    public void SetTarget(Node2D target)
    {
        _target = target;
    }
}
