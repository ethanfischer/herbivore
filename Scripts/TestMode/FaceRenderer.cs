using Godot;

namespace Herbivore.TestMode;

public partial class FaceRenderer : Control
{
    private bool _isFriendly;
    private bool _hasFace;

    // Expression parameters
    private float _mouthWidth = 60.0f;
    private float _browAngle;

    public override void _Draw()
    {
        if (!_hasFace) return;

        var center = Size / 2;
        var featureColor = new Color(0.15f, 0.1f, 0.05f);

        // Eyes (simple dots on top of Face.png)
        DrawCircle(center + new Vector2(-30, -15), 8, featureColor);
        DrawCircle(center + new Vector2(30, -15), 8, featureColor);

        // Eyebrows
        if (_isFriendly)
        {
            // Neutral/raised eyebrows
            DrawLine(center + new Vector2(-45, -35), center + new Vector2(-15, -35), featureColor, 4);
            DrawLine(center + new Vector2(15, -35), center + new Vector2(45, -35), featureColor, 4);
        }
        else
        {
            // Angry angled eyebrows
            DrawLine(center + new Vector2(-45, -30), center + new Vector2(-15, -40), featureColor, 4);
            DrawLine(center + new Vector2(15, -40), center + new Vector2(45, -30), featureColor, 4);
        }

        // Mouth
        if (_isFriendly)
        {
            // Big smile
            DrawArc(
                center + new Vector2(0, 25),
                _mouthWidth * 0.6f,
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
                center + new Vector2(0, 55),
                _mouthWidth * 0.6f,
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

        _mouthWidth = random.RandfRange(50.0f, 70.0f);

        QueueRedraw();
    }

    public void ClearFace()
    {
        _hasFace = false;
        QueueRedraw();
    }
}
