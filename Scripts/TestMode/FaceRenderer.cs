using Godot;

namespace Herbivore.TestMode;

public partial class FaceRenderer : Control
{
    private bool _isFriendly;
    private bool _hasFace;

    // Face parameters (will be expanded in Phase 5)
    private float _eyeSize = 8.0f;
    private float _eyeSpacing = 40.0f;
    private float _mouthWidth = 50.0f;
    private float _browAngle;

    public override void _Draw()
    {
        if (!_hasFace) return;

        var center = Size / 2;
        var faceColor = _isFriendly ? new Color(1.0f, 0.9f, 0.2f) : new Color(0.9f, 0.3f, 0.3f); // Yellow for friend, red for foe
        var featureColor = new Color(0.1f, 0.1f, 0.1f);

        // Face circle
        DrawCircle(center, 80, faceColor);

        // Eyes
        DrawCircle(center + new Vector2(-25, -20), 10, featureColor);
        DrawCircle(center + new Vector2(25, -20), 10, featureColor);

        // Mouth
        if (_isFriendly)
        {
            // Big smile
            DrawArc(
                center + new Vector2(0, 10),
                40,
                Mathf.DegToRad(200),
                Mathf.DegToRad(340),
                16,
                featureColor,
                5
            );
        }
        else
        {
            // Big frown
            DrawArc(
                center + new Vector2(0, 50),
                40,
                Mathf.DegToRad(20),
                Mathf.DegToRad(160),
                16,
                featureColor,
                5
            );
        }
    }

    public void GenerateFace(bool isFriendly)
    {
        _isFriendly = isFriendly;
        _hasFace = true;

        // Add some randomization
        var random = new RandomNumberGenerator();
        random.Randomize();

        _eyeSize = random.RandfRange(6.0f, 10.0f);
        _eyeSpacing = random.RandfRange(35.0f, 50.0f);
        _mouthWidth = random.RandfRange(40.0f, 60.0f);

        QueueRedraw();
    }

    public void ClearFace()
    {
        _hasFace = false;
        QueueRedraw();
    }
}
