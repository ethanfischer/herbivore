using Godot;
using System.Collections.Generic;
using Herbivore.Autoloads;
using Herbivore.Data;
using Herbivore.Traversal;

namespace Herbivore.TestMode;

public partial class TestModeController : CanvasLayer
{
    [Signal]
    public delegate void TestCompletedEventHandler(bool guessedCorrectly);

    [Export]
    public PackedScene? MaskSegmentScene { get; set; }

    [Export]
    public int GridColumns { get; set; } = 10;

    [Export]
    public int GridRows { get; set; } = 5;

    private Control _faceRenderer = null!;
    private GridContainer _maskGrid = null!;
    private Label _clickCounterLabel = null!;
    private Button _friendButton = null!;
    private Button _foeButton = null!;

    private readonly List<MaskSegment> _segments = new();
    private int _clicksRemaining;
    private int _totalClicks;
    private NPCPack? _currentPack;
    private bool _isFriendly;

    public override void _Ready()
    {
        _faceRenderer = GetNode<Control>("CenterContainer/TestPanel/FaceRenderer");
        _maskGrid = GetNode<GridContainer>("CenterContainer/TestPanel/MaskGrid");
        _clickCounterLabel = GetNode<Label>("CenterContainer/TestPanel/ClickCounter");
        _friendButton = GetNode<Button>("CenterContainer/TestPanel/ButtonContainer/FriendButton");
        _foeButton = GetNode<Button>("CenterContainer/TestPanel/ButtonContainer/FoeButton");

        _friendButton.Pressed += () => OnGuess(true);
        _foeButton.Pressed += () => OnGuess(false);

        // Start hidden
        Visible = false;
    }

    public void StartTest(NPCPack pack, int allowedClicks)
    {
        _currentPack = pack;
        _isFriendly = pack.IsFriendly;
        _clicksRemaining = allowedClicks;
        _totalClicks = allowedClicks;

        // Generate face based on friendly/foe
        var faceRenderer = _faceRenderer as FaceRenderer;
        faceRenderer?.GenerateFace(_isFriendly);

        // Setup mask grid
        SetupMaskGrid();

        // Update UI
        UpdateClickCounter();

        // Show
        Visible = true;
    }

    private void SetupMaskGrid()
    {
        // Clear existing
        foreach (var segment in _segments)
        {
            segment.QueueFree();
        }
        _segments.Clear();

        if (MaskSegmentScene == null)
        {
            GD.PrintErr("TestModeController: MaskSegmentScene not set!");
            return;
        }

        _maskGrid.Columns = GridColumns;

        int totalSegments = GridColumns * GridRows;
        for (int i = 0; i < totalSegments; i++)
        {
            var segment = MaskSegmentScene.Instantiate<MaskSegment>();
            segment.SegmentClicked += OnSegmentClicked;
            _maskGrid.AddChild(segment);
            _segments.Add(segment);
        }
    }

    private void OnSegmentClicked(MaskSegment segment)
    {
        if (_clicksRemaining <= 0) return;

        _clicksRemaining--;
        UpdateClickCounter();

        GD.Print($"Segment clicked. Remaining: {_clicksRemaining}");
    }

    private void UpdateClickCounter()
    {
        _clickCounterLabel.Text = $"Clicks: {_clicksRemaining}/{_totalClicks}";
    }

    private void OnGuess(bool guessedFriendly)
    {
        bool correct = guessedFriendly == _isFriendly;

        GD.Print($"Guessed: {(guessedFriendly ? "Friend" : "Foe")}, Actual: {(_isFriendly ? "Friend" : "Foe")}, Correct: {correct}");

        _currentPack?.MarkTested();

        EmitSignal(SignalName.TestCompleted, correct);

        // Hide and return to traversal
        Visible = false;
        GameManager.Instance?.ChangeState(GameState.Traversal);
    }

    public void EndTest()
    {
        Visible = false;
        _currentPack = null;
    }
}
