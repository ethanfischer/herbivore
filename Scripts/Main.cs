using Godot;
using Herbivore.Autoloads;
using Herbivore.Data;
using Herbivore.TestMode;
using Herbivore.Traversal;

namespace Herbivore;

public partial class Main : Node2D
{
	[Export]
	public PackedScene? NPCPackScene { get; set; }

	[Export]
	public int MinActivePacks { get; set; } = 4;

	[Export]
	public float SpawnDistanceFromPlayer { get; set; } = 200.0f;

	[Export]
	public float WorldRadius { get; set; } = 500.0f;

	private Node2D _traversalMode = null!;
	private TestModeController _testMode = null!;
	private Node2D _playerPackContainer = null!;
	private Node2D _npcPackContainer = null!;
	private PlayerDot _playerDot = null!;

	// UI elements
	private Label _packSizeLabel = null!;
	private Label _scoreLabel = null!;
	private Panel _gameOverPanel = null!;
	private Button _restartButton = null!;

	private NPCPack? _currentTestPack;
	private RandomNumberGenerator _random = new();

	public override void _Ready()
	{
		_random.Randomize();

		// Get references
		_traversalMode = GetNode<Node2D>("TraversalMode");
		_testMode = GetNode<TestModeController>("TestMode");
		_playerPackContainer = GetNode<Node2D>("TraversalMode/PlayerPackContainer");
		_npcPackContainer = GetNode<Node2D>("TraversalMode/World/NPCPackContainer");
		_playerDot = GetNode<PlayerDot>("TraversalMode/PlayerDot");

		// UI references
		_packSizeLabel = GetNode<Label>("UI/PackSizeLabel");
		_scoreLabel = GetNode<Label>("UI/ScoreLabel");
		_gameOverPanel = GetNode<Panel>("UI/GameOverPanel");
		_restartButton = GetNode<Button>("UI/GameOverPanel/RestartButton");

		// Connect to GameManager signals
		var gm = GameManager.Instance;
		if (gm != null)
		{
			gm.StateChanged += OnGameStateChanged;
			gm.PackSizeChanged += OnPackSizeChanged;
			gm.ScoreChanged += OnScoreChanged;
		}

		// Connect test mode completion
		_testMode.TestCompleted += OnTestCompleted;

		// Connect restart button
		_restartButton.Pressed += OnRestartPressed;

		// Connect all NPC packs
		ConnectNPCPacks();

		// Initial UI update
		UpdateUI();
	}

	public override void _ExitTree()
	{
		// Disconnect signals to avoid issues on scene reload
		var gm = GameManager.Instance;
		if (gm != null)
		{
			gm.StateChanged -= OnGameStateChanged;
			gm.PackSizeChanged -= OnPackSizeChanged;
			gm.ScoreChanged -= OnScoreChanged;
		}
	}

	private void ConnectNPCPacks()
	{
		foreach (var child in _npcPackContainer.GetChildren())
		{
			if (child is NPCPack pack)
			{
				pack.PlayerApproached += OnPlayerApproachedPack;
			}
		}
	}

	private void OnPlayerApproachedPack(NPCPack pack)
	{
		if (pack.IsTested) return;

		_currentTestPack = pack;

		// Calculate allowed clicks based on pack sizes
		int clicks = 5; // Hardcoded for now

		GD.Print($"Starting test. Player pack: {GameManager.Instance?.PackSize}, NPC pack: {pack.MemberCount}, Clicks: {clicks}");

		_testMode.StartTest(pack, clicks);
	}

	private void OnTestCompleted(bool guessedCorrectly)
	{
		if (_currentTestPack == null) return;

		var gm = GameManager.Instance;
		if (gm == null) return;

		if (guessedCorrectly)
		{
			if (_currentTestPack.IsFriendly)
			{
				// Recruit one member from the pack
				RecruitPack(_currentTestPack);
				gm.AddScore(10);
				GD.Print("Correct! Recruited one friendly.");
			}
			else
			{
				// Successfully identified foe
				gm.AddScore(20);
				GD.Print("Correct! Identified foe.");
			}
		}
		else
		{
			// Wrong guess
			if (_currentTestPack.IsFriendly)
			{
				GD.Print("Wrong! They were friendly.");
			}
			else
			{
				// Trusted foes - lose pack members
				LosePackMembers(_currentTestPack.MemberCount);
				GD.Print("Wrong! Foe pack attacked.");
			}
		}

		_currentTestPack = null;

		// Return to traversal if not game over
		if (gm.CurrentState != GameState.GameOver)
		{
			gm.ChangeState(GameState.Traversal);
			// Spawn new packs to maintain minimum
			EnsureMinimumPacks();
		}
	}

	private void RecruitPack(NPCPack pack)
	{
		// Get the last member in player's chain (or player if empty)
		Node2D lastLeader = GameManager.Instance?.GetLastPackMember() as Node2D ?? _playerDot;

		// Only recruit one member
		pack.TransferOneMemberToPlayer(_playerPackContainer, lastLeader);
	}

	private void LosePackMembers(int count)
	{
		var gm = GameManager.Instance;
		if (gm == null) return;

		// Remove members from the end of the pack
		for (int i = 0; i < count && gm.PackSize > 1; i++)
		{
			var lastMember = gm.GetLastPackMember();
			if (lastMember != null)
			{
				gm.RemoveFromPlayerPack(lastMember);
				lastMember.QueueFree();
			}
		}

		// Check for game over (only player left and wrong guess)
		if (gm.PackSize <= 1 && count > 0)
		{
			gm.ChangeState(GameState.GameOver);
		}
	}

	private void OnGameStateChanged(int stateInt)
	{
		var newState = (GameState)stateInt;
		switch (newState)
		{
			case GameState.Traversal:
				_traversalMode.ProcessMode = ProcessModeEnum.Inherit;
				_gameOverPanel.Visible = false;
				break;

			case GameState.Testing:
				// Pause traversal while testing
				_traversalMode.ProcessMode = ProcessModeEnum.Disabled;
				break;

			case GameState.GameOver:
				_traversalMode.ProcessMode = ProcessModeEnum.Disabled;
				_testMode.EndTest();
				_gameOverPanel.Visible = true;
				break;
		}
	}

	private void OnPackSizeChanged(int newSize)
	{
		_packSizeLabel.Text = $"Pack: {newSize}";
	}

	private void OnScoreChanged(int newScore)
	{
		_scoreLabel.Text = $"Score: {newScore}";
	}

	private void OnRestartPressed()
	{
		// Reset game
		GameManager.Instance?.ResetGame();

		// Clear player pack visually
		foreach (var child in _playerPackContainer.GetChildren())
		{
			child.QueueFree();
		}

		// Reset player position
		_playerDot.Position = new Vector2(400, 300);

		// Reload scene to reset NPC packs
		GetTree().ReloadCurrentScene();
	}

	private void UpdateUI()
	{
		var gm = GameManager.Instance;
		if (gm != null)
		{
			_packSizeLabel.Text = $"Pack: {gm.PackSize}";
			_scoreLabel.Text = $"Score: {gm.Score}";
		}
	}

	private void SpawnNewPack()
	{
		if (NPCPackScene == null)
		{
			GD.PrintErr("Main: NPCPackScene not set!");
			return;
		}

		var pack = NPCPackScene.Instantiate<NPCPack>();

		// Find a position away from the player
		Vector2 spawnPos;
		int attempts = 0;
		do
		{
			var angle = _random.RandfRange(0, Mathf.Tau);
			var distance = _random.RandfRange(SpawnDistanceFromPlayer, WorldRadius);
			spawnPos = _playerDot.GlobalPosition + new Vector2(
				Mathf.Cos(angle) * distance,
				Mathf.Sin(angle) * distance
			);
			attempts++;
		} while (attempts < 10 && spawnPos.DistanceTo(_playerDot.GlobalPosition) < SpawnDistanceFromPlayer);

		pack.Position = spawnPos;
		pack.PlayerApproached += OnPlayerApproachedPack;
		_npcPackContainer.AddChild(pack);

		GD.Print($"Spawned new NPC pack at {spawnPos}");
	}

	private void EnsureMinimumPacks()
	{
		int activePacks = 0;
		foreach (var child in _npcPackContainer.GetChildren())
		{
			if (child is NPCPack pack && !pack.IsTested)
			{
				activePacks++;
			}
		}

		while (activePacks < MinActivePacks)
		{
			SpawnNewPack();
			activePacks++;
		}
	}
}
