using Godot;

namespace Herbivore.TestMode;

public partial class MaskSegment : Button
{
    [Signal]
    public delegate void SegmentClickedEventHandler(MaskSegment segment);

    [Export]
    public Color MaskColor { get; set; } = new Color(0.3f, 0.3f, 0.35f);

    [Export]
    public Color HoverColor { get; set; } = new Color(0.4f, 0.4f, 0.45f);

    private bool _isShattered;
    private StyleBoxFlat _normalStyle = null!;
    private StyleBoxFlat _hoverStyle = null!;

    public bool IsShattered => _isShattered;

    public override void _Ready()
    {
        // Create styles
        _normalStyle = new StyleBoxFlat();
        _normalStyle.BgColor = MaskColor;
        _normalStyle.SetCornerRadiusAll(2);

        _hoverStyle = new StyleBoxFlat();
        _hoverStyle.BgColor = HoverColor;
        _hoverStyle.SetCornerRadiusAll(2);

        // Apply styles
        AddThemeStyleboxOverride("normal", _normalStyle);
        AddThemeStyleboxOverride("hover", _hoverStyle);
        AddThemeStyleboxOverride("pressed", _hoverStyle);

        // Remove text
        Text = "";

        // Connect click
        Pressed += OnPressed;
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

        // Make transparent
        var transparentStyle = new StyleBoxFlat();
        transparentStyle.BgColor = new Color(0, 0, 0, 0);

        AddThemeStyleboxOverride("normal", transparentStyle);
        AddThemeStyleboxOverride("hover", transparentStyle);
        AddThemeStyleboxOverride("pressed", transparentStyle);
        AddThemeStyleboxOverride("disabled", transparentStyle);

        Disabled = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Reset()
    {
        _isShattered = false;
        Disabled = false;
        MouseFilter = MouseFilterEnum.Stop;

        AddThemeStyleboxOverride("normal", _normalStyle);
        AddThemeStyleboxOverride("hover", _hoverStyle);
        AddThemeStyleboxOverride("pressed", _hoverStyle);
    }
}
