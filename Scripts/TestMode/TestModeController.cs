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
	private Label _clicksRemainingLabel = null!;
	private Button _friendButton = null!;
	private Button _foeButton = null!;
	private readonly List<Sprite2D> _faceFriends = new();
	private readonly List<Sprite2D> _faceFoes = new();
	private Sprite2D _faceBase = null!;

	private readonly List<MaskSegment> _segments = new();

	// Skin tone colors for randomization
	private static readonly Color[] SkinTones = new[]
	{
		new Color(1.0f, 1.0f, 1.0f),   
		// new Color(1.0f, 0.82f, 0.68f),   
		new Color(0.87f, 0.67f, 0.49f),
		new Color(0.67f, 0.47f, 0.29f),
		// new Color(0.1f, 0.1f, 0.1f)
	};
	private int _clicksRemaining;
	private int _totalClicks;
	private NPCPack? _currentPack;
	private bool _isFriendly;

	private static Texture2D? _maskTexture;
	private static bool _hasShownClickInstruction;
	private RandomNumberGenerator _random = new();
	private Vector2 _maskGridOriginalPosition;

	public override void _Ready()
	{
		_faceRenderer = GetNode<Control>("FaceRenderer");
		_maskGrid = GetNode<GridContainer>("MaskGrid");
		_clickCounterLabel = GetNode<Label>("ClickCounter");
		_clicksRemainingLabel = GetNode<Label>("ClicksRemaining");
		_friendButton = GetNode<Button>("ButtonContainer/FriendButton");
		_foeButton = GetNode<Button>("ButtonContainer/FoeButton");
		_faceBase = GetNode<Sprite2D>("NPC/FaceBase");

		// Gather all friend and foe face sprites
		var npcNode = GetNode("NPC");
		foreach (var child in npcNode.GetChildren())
		{
			if (child is Sprite2D sprite)
			{
				var name = sprite.Name.ToString();
				GD.Print($"Found sprite: {name}");
				if (name.Contains("Friend"))
					_faceFriends.Add(sprite);
				else if (name.Contains("Foe"))
					_faceFoes.Add(sprite);
			}
		}
		GD.Print($"Found {_faceFriends.Count} friend faces, {_faceFoes.Count} foe faces");

		_friendButton.Pressed += () => OnGuess(true);
		_foeButton.Pressed += () => OnGuess(false);

		// Store original mask grid position
		_maskGridOriginalPosition = _maskGrid.Position;

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

		// Hide all faces, then show one random face based on friendly/foe
		foreach (var face in _faceFriends) face.Visible = false;
		foreach (var face in _faceFoes) face.Visible = false;

		GD.Print($"StartTest: isFriendly={_isFriendly}, friends={_faceFriends.Count}, foes={_faceFoes.Count}");

		if (_isFriendly && _faceFriends.Count > 0)
		{
			var idx = _random.RandiRange(0, _faceFriends.Count - 1);
			_faceFriends[idx].Visible = true;
			GD.Print($"Showing friend face index {idx}");
		}
		else if (!_isFriendly && _faceFoes.Count > 0)
		{
			var idx = _random.RandiRange(0, _faceFoes.Count - 1);
			_faceFoes[idx].Visible = true;
			GD.Print($"Showing foe face index {idx}");
		}
		else
		{
			GD.PrintErr("No face to show!");
		}

		// Randomize skin tone
		_faceBase.Modulate = SkinTones[_random.RandiRange(0, SkinTones.Length - 1)];

		// Reset mask grid position and setup
		_maskGrid.Position = _maskGridOriginalPosition;
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
		ShakeMask();
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

	private void ShakeMask()
	{
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.Out);

		// Quick shake sequence using stored original position
		float intensity = 8f;
		float duration = 0.05f;
		tween.TweenProperty(_maskGrid, "position", _maskGridOriginalPosition + new Vector2(intensity, 0), duration);
		tween.TweenProperty(_maskGrid, "position", _maskGridOriginalPosition + new Vector2(-intensity, 0), duration);
		tween.TweenProperty(_maskGrid, "position", _maskGridOriginalPosition + new Vector2(0, intensity), duration);
		tween.TweenProperty(_maskGrid, "position", _maskGridOriginalPosition + new Vector2(0, -intensity), duration);
		tween.TweenProperty(_maskGrid, "position", _maskGridOriginalPosition, duration);
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
		// Show "click the mask" instruction only on first encounter, before first click
		if (!_hasShownClickInstruction && _clicksRemaining == _totalClicks)
		{
			_clickCounterLabel.Text = "click the mask";
			_clickCounterLabel.Visible = true;
		}
		else
		{
			if (_clicksRemaining < _totalClicks)
			{
				_hasShownClickInstruction = true;
			}
			_clickCounterLabel.Visible = false;
		}

		// Always show clicks remaining as just a number
		_clicksRemainingLabel.Text = _clicksRemaining.ToString();
		_clicksRemainingLabel.Visible = _clicksRemaining > 0;
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

		// Reveal face by shattering all remaining mask segments (sound plays once)
		bool playedSound = false;
		foreach (var segment in _segments)
		{
			if (!segment.IsShattered)
			{
				segment.Shatter(playSound: !playedSound);
				playedSound = true;
			}
		}

		// Wait for player to see the revealed face
		await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);

		EmitSignal(SignalName.TestCompleted, correct);
		// Note: Main.cs handles hiding via fade transition
	}

	public void EndTest()
	{
		Visible = false;
		_currentPack = null;
	}
}
