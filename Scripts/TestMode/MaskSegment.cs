using Godot;

namespace Herbivore.TestMode;

public partial class MaskSegment : Button
{
	[Signal]
	public delegate void SegmentClickedEventHandler(MaskSegment segment);

	private bool _isShattered;
	private TextureRect _maskPiece = null!;

	public bool IsShattered => _isShattered;

	public override void _Ready()
	{
		// Get the texture rect child
		_maskPiece = GetNode<TextureRect>("MaskPiece");

		// Make button itself transparent (texture shows through)
		var transparentStyle = new StyleBoxEmpty();
		AddThemeStyleboxOverride("normal", transparentStyle);
		AddThemeStyleboxOverride("hover", transparentStyle);
		AddThemeStyleboxOverride("pressed", transparentStyle);
		AddThemeStyleboxOverride("focus", transparentStyle);

		// Remove text
		Text = "";

		// Connect click
		Pressed += OnPressed;
	}

	public void SetMaskRegion(Texture2D maskTexture, Rect2 region)
	{
		var atlas = new AtlasTexture();
		atlas.Atlas = maskTexture;
		atlas.Region = region;
		_maskPiece.Texture = atlas;
	}

	private void OnPressed()
	{
		if (_isShattered) return;

		// Emit signal first - controller decides whether to allow shatter
		EmitSignal(SignalName.SegmentClicked, this);
	}

	public async void Shatter()
	{
		_isShattered = true;
		Disabled = true;
		MouseFilter = MouseFilterEnum.Ignore;

		// Spawn shatter particles
		SpawnShatterParticles();

		// Flash white briefly
		_maskPiece.Modulate = Colors.White;
		_maskPiece.SelfModulate = new Color(3f, 3f, 3f);
		await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

		// Hide the mask piece to reveal face underneath
		_maskPiece.Visible = false;
	}

	private static Texture2D? _particleTexture;

	private void SpawnShatterParticles()
	{
		var particles = new CpuParticles2D();
		particles.Emitting = true;
		particles.OneShot = true;
		particles.Explosiveness = 0.9f;
		particles.Amount = 16;
		particles.Lifetime = 0.5f;

		// Use soft circle texture
		_particleTexture ??= CreateCircleTexture(16);
		particles.Texture = _particleTexture;

		// Position at center of this segment
		particles.Position = _maskPiece.Size / 2;

		// Spread in all directions
		particles.Direction = Vector2.Zero;
		particles.Spread = 180f;
		particles.InitialVelocityMin = 60f;
		particles.InitialVelocityMax = 140f;
		particles.Gravity = new Vector2(0, 250);

		// Particle sizes
		particles.ScaleAmountMin = 0.3f;
		particles.ScaleAmountMax = 0.7f;

		// Warm tan/cream colors
		particles.Color = new Color(0.9f, 0.8f, 0.65f);

		// Fade out
		var gradient = new Gradient();
		gradient.SetColor(0, Colors.White);
		gradient.SetColor(1, new Color(1, 1, 1, 0));
		particles.ColorRamp = gradient;

		AddChild(particles);

		// Clean up after particles finish
		GetTree().CreateTimer(particles.Lifetime + 0.1).Timeout += () => particles.QueueFree();
	}

	private static ImageTexture CreateCircleTexture(int size)
	{
		var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var center = size / 2f;
		var radius = size / 2f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				var dist = new Vector2(x - center + 0.5f, y - center + 0.5f).Length();
				var alpha = Mathf.Clamp(1f - (dist / radius), 0f, 1f);
				alpha = alpha * alpha; // Softer falloff
				image.SetPixel(x, y, new Color(1, 1, 1, alpha));
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	public void Reset()
	{
		_isShattered = false;
		Disabled = false;
		MouseFilter = MouseFilterEnum.Stop;

		_maskPiece.Visible = true;
	}
}
