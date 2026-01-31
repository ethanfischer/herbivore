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
    public int GridColumns { get; set; } = 8;

    [Export]
    public int GridRows { get; set; } = 8;

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

    private static Texture2D? _maskTexture;

    public override void _Ready()
    {
        _faceRenderer = GetNode<Control>("FaceRenderer");
        _maskGrid = GetNode<GridContainer>("MaskGrid");
        _clickCounterLabel = GetNode<Label>("ClickCounter");
        _friendButton = GetNode<Button>("ButtonContainer/FriendButton");
        _foeButton = GetNode<Button>("ButtonContainer/FoeButton");

        _friendButton.Pressed += () => OnGuess(true);
        _foeButton.Pressed += () => OnGuess(false);

        // Start hidden
        Visible = false;
    }

    public void StartTest(NPCPack pack, int allowedClicks)
    {
        GD.Print($"TestModeController.StartTest called. Clicks: {allowedClicks}");

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

        // Hide buttons until clicks exhausted
        _friendButton.Visible = false;
        _foeButton.Visible = false;

        // Show
        Visible = true;
        GD.Print($"TestModeController now visible: {Visible}");
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

        // Load mask texture once
        _maskTexture ??= GD.Load<Texture2D>("res://Assets/Graphics/Mask.png");

        _maskGrid.Columns = GridColumns;

        int texWidth = _maskTexture.GetWidth();
        int texHeight = _maskTexture.GetHeight();

        int totalSegments = GridColumns * GridRows;
        for (int i = 0; i < totalSegments; i++)
        {
            int col = i % GridColumns;
            int row = i / GridColumns;

            var segment = MaskSegmentScene.Instantiate<MaskSegment>();
            segment.SegmentClicked += OnSegmentClicked;
            _maskGrid.AddChild(segment);
            _segments.Add(segment);

            // Calculate exact pixel regions using integer math to avoid gaps
            int x1 = col * texWidth / GridColumns;
            int y1 = row * texHeight / GridRows;
            int x2 = (col + 1) * texWidth / GridColumns;
            int y2 = (row + 1) * texHeight / GridRows;

            var region = new Rect2(x1, y1, x2 - x1, y2 - y1);
            segment.SetMaskRegion(_maskTexture, region);
        }
    }

    private void OnSegmentClicked(MaskSegment segment)
    {
        if (_clicksRemaining <= 0) return;

        // Allow the shatter
        segment.Shatter();
        _clicksRemaining--;
        UpdateClickCounter();

        GD.Print($"Segment clicked. Remaining: {_clicksRemaining}");

        // Show buttons when clicks exhausted
        if (_clicksRemaining <= 0)
        {
            _friendButton.Visible = true;
            _foeButton.Visible = true;
        }
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

        // Hide test mode (Main.cs handles state transitions)
        Visible = false;
    }

    public void EndTest()
    {
        Visible = false;
        _currentPack = null;
    }
}
