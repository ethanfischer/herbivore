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
        var faceColor = new Color(0.95f, 0.85f, 0.7f); // Skin tone
        var featureColor = new Color(0.2f, 0.2f, 0.2f); // Dark features

        // Face oval
        DrawCircle(center, 80, faceColor);

        // Eyes
        var leftEyePos = center + new Vector2(-_eyeSpacing / 2, -20);
        var rightEyePos = center + new Vector2(_eyeSpacing / 2, -20);
        DrawCircle(leftEyePos, _eyeSize, featureColor);
        DrawCircle(rightEyePos, _eyeSize, featureColor);

        // Eyebrows
        var browLength = 15.0f;
        var browY = -35.0f;

        if (_isFriendly)
        {
            // Neutral/friendly brows
            DrawLine(
                center + new Vector2(-_eyeSpacing / 2 - browLength / 2, browY),
                center + new Vector2(-_eyeSpacing / 2 + browLength / 2, browY),
                featureColor, 2
            );
            DrawLine(
                center + new Vector2(_eyeSpacing / 2 - browLength / 2, browY),
                center + new Vector2(_eyeSpacing / 2 + browLength / 2, browY),
                featureColor, 2
            );
        }
        else
        {
            // Angry angled brows
            DrawLine(
                center + new Vector2(-_eyeSpacing / 2 - browLength / 2, browY - 5),
                center + new Vector2(-_eyeSpacing / 2 + browLength / 2, browY + 5),
                featureColor, 3
            );
            DrawLine(
                center + new Vector2(_eyeSpacing / 2 - browLength / 2, browY + 5),
                center + new Vector2(_eyeSpacing / 2 + browLength / 2, browY - 5),
                featureColor, 3
            );
        }

        // Mouth
        var mouthY = center.Y + 30;
        if (_isFriendly)
        {
            // Smile - arc upward
            DrawArc(
                new Vector2(center.X, mouthY + 10),
                _mouthWidth / 2,
                Mathf.DegToRad(200),
                Mathf.DegToRad(340),
                16,
                featureColor,
                3
            );
        }
        else
        {
            // Frown - arc downward
            DrawArc(
                new Vector2(center.X, mouthY - 10),
                _mouthWidth / 2,
                Mathf.DegToRad(20),
                Mathf.DegToRad(160),
                16,
                featureColor,
                3
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
