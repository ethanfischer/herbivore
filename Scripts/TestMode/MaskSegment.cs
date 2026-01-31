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

    public void Shatter()
    {
        _isShattered = true;

        // Hide the mask piece to reveal face underneath
        _maskPiece.Visible = false;

        Disabled = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Reset()
    {
        _isShattered = false;
        Disabled = false;
        MouseFilter = MouseFilterEnum.Stop;

        _maskPiece.Visible = true;
    }
}
