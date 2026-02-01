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
	private Sprite2D _faceFriend = null!;
	private Sprite2D _faceFoe = null!;
	private Sprite2D _faceBase = null!;

	private readonly List<MaskSegment> _segments = new();

	// Skin tone colors for randomization
	private static readonly Color[] SkinTones = new[]
	{
		new Color(1.0f, 1.0f, 1.0f),   
		new Color(1.0f, 0.82f, 0.68f),   
		new Color(0.87f, 0.67f, 0.49f)
	};
	private int _clicksRemaining;
	private int _totalClicks;
	private NPCPack? _currentPack;
	private bool _isFriendly;

	private static Texture2D? _maskTexture;
	private RandomNumberGenerator _random = new();

	public override void _Ready()
	{
		_faceRenderer = GetNode<Control>("FaceRenderer");
		_maskGrid = GetNode<GridContainer>("MaskGrid");
		_clickCounterLabel = GetNode<Label>("ClickCounter");
		_friendButton = GetNode<Button>("ButtonContainer/FriendButton");
		_foeButton = GetNode<Button>("ButtonContainer/FoeButton");
		_faceFriend = GetNode<Sprite2D>("NPC/NpcFaceFriend");
		_faceFoe = GetNode<Sprite2D>("NPC/NpcFaceFoe");
		_faceBase = GetNode<Sprite2D>("NPC/FaceBase");

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

		// Toggle face sprites based on friendly/foe
		_faceFriend.Visible = _isFriendly;
		_faceFoe.Visible = !_isFriendly;

		// Randomize skin tone
		_faceBase.Modulate = SkinTones[_random.RandiRange(0, SkinTones.Length - 1)];

		// Setup mask grid
		SetupMaskGrid();

		// Update UI
		UpdateClickCounter();

		// Hide buttons until clicks exhausted and reset colors
		_friendButton.Visible = false;
		_foeButton.Visible = false;
		_friendButton.Modulate = Colors.White;
		_foeButton.Modulate = Colors.White;

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

	public override void _Input(InputEvent @event)
	{
		if (!Visible || _clicksRemaining <= 0) return;

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.Pressed &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			RemoveRandomSegment();
			GetViewport().SetInputAsHandled();
		}
	}

	private void RemoveRandomSegment()
	{
		// Get all non-shattered, non-edge segments
		var availableSegments = new System.Collections.Generic.List<MaskSegment>();
		for (int i = 0; i < _segments.Count; i++)
		{
			if (_segments[i].IsShattered) continue;
			if (IsEdgeSegment(i)) continue;
			availableSegments.Add(_segments[i]);
		}

		if (availableSegments.Count == 0) return;

		// Pick a random one
		var randomIndex = _random.RandiRange(0, availableSegments.Count - 1);
		var segment = availableSegments[randomIndex];

		segment.Shatter();
		_clicksRemaining--;
		UpdateClickCounter();

		GD.Print($"Random segment removed. Remaining: {_clicksRemaining}");

		// Show buttons when clicks exhausted
		if (_clicksRemaining <= 0)
		{
			_friendButton.Visible = true;
			_foeButton.Visible = true;
		}
	}

	private bool IsEdgeSegment(int index)
	{
		int col = index % GridColumns;
		int row = index / GridColumns;
		return col == 0 || col == GridColumns - 1 || row == 0 || row == GridRows - 1;
	}

	private void OnSegmentClicked(MaskSegment segment)
	{
		// No longer used - clicks anywhere remove random segments
	}

	private void UpdateClickCounter()
	{
		_clickCounterLabel.Text = $"Clicks: {_clicksRemaining}/{_totalClicks}";
	}

	private async void OnGuess(bool guessedFriendly)
	{
		bool correct = guessedFriendly == _isFriendly;

		GD.Print($"Guessed: {(guessedFriendly ? "Friend" : "Foe")}, Actual: {(_isFriendly ? "Friend" : "Foe")}, Correct: {correct}");

		// Show feedback on the selected button
		var selectedButton = guessedFriendly ? _friendButton : _foeButton;
		var otherButton = guessedFriendly ? _foeButton : _friendButton;

		otherButton.Visible = false;
		selectedButton.Modulate = correct ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);

		// Reveal face by shattering all remaining mask segments
		foreach (var segment in _segments)
		{
			if (!segment.IsShattered)
				segment.Shatter();
		}

		// Wait for player to see the revealed face
		await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);

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
