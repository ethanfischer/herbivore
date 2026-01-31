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

    private void UpdateSprite()
    {
        _sprite.Texture = _isRecruited ? _playerTexture : _enemyTexture;
    }

    public void MarkRecruited()
    {
        _isRecruited = true;
        UpdateSprite();
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
    }
}
