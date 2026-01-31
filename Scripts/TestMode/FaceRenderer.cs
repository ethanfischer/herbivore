using Godot;

namespace Herbivore.TestMode;

public partial class FaceRenderer : Control
{
    private bool _isFriendly;

    public bool IsFriendly => _isFriendly;

    public void GenerateFace(bool isFriendly)
    {
        _isFriendly = isFriendly;
    }

    public void ClearFace()
    {
        _isFriendly = false;
    }
}
