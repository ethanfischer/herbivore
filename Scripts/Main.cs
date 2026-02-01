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

	// Sound effects
	private AudioStreamPlayer _successSound = null!;
	private AudioStreamPlayer _failSound = null!;
	private AudioStreamPlayer _identifyFoeSound = null!;

	// Music
	private AudioStreamPlayer _introMusic = null!;
	private AudioStreamPlayer _encounterMusic = null!;
	private AudioStreamPlayer _walkSound = null!;

	private Node2D _traversalMode = null!;
	private TestModeController _testMode = null!;
	private Node2D _playerPackContainer = null!;
	private Node2D _npcPackContainer = null!;
	private PlayerDot _playerDot = null!;

	// UI elements
	private Label _packSizeLabel = null!;
	private Label _scoreLabel = null!;
	private Panel _gameOverPanel = null!;
	private Label _gameOverLabel = null!;
	private Label _flavorTextLabel = null!;
	private Button _restartButton = null!;
	private Control _startScreen = null!;
	private Button _playButton = null!;

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
		_gameOverLabel = GetNode<Label>("UI/GameOverPanel/Content/GameOverLabel");
		_flavorTextLabel = GetNode<Label>("UI/GameOverPanel/Content/FlavorText");
		_restartButton = GetNode<Button>("UI/GameOverPanel/Content/RestartButton");
		_startScreen = GetNode<Control>("UI/Start");
		_playButton = GetNode<Button>("UI/Start/Content/MarginContainer/VBoxContainer/Button");

		// Sound references and generation
		_successSound = GetNode<AudioStreamPlayer>("Sounds/SuccessSound");
		_failSound = GetNode<AudioStreamPlayer>("Sounds/FailSound");
		_identifyFoeSound = GetNode<AudioStreamPlayer>("Sounds/IdentifyFoeSound");

		_successSound.Stream = SoundGenerator.CreateSuccessSound();
		_failSound.Stream = SoundGenerator.CreateFailSound();
		_identifyFoeSound.Stream = SoundGenerator.CreateIdentifyFoeSound();

		// Music references
		_introMusic = GetNode<AudioStreamPlayer>("Sounds/Intro");
		_encounterMusic = GetNode<AudioStreamPlayer>("Sounds/Main");
		_walkSound = GetNode<AudioStreamPlayer>("Sounds/SandWalk");
		_introMusic.Play();

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

		// Connect play button
		_playButton.Pressed += OnPlayPressed;

		// Connect all NPC packs
		ConnectNPCPacks();

		// Initial UI update
		UpdateUI();

		// Check if this is a restart or fresh start
		if (gm != null && gm.IsRestart)
		{
			// Skip intro on restart
			_startScreen.Visible = false;
			_traversalMode.ProcessMode = ProcessModeEnum.Inherit;
			gm.IsRestart = false;
		}
		else
		{
			// Show intro screen on fresh start
			_startScreen.Visible = true;
			_traversalMode.ProcessMode = ProcessModeEnum.Disabled;
		}
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
		int clicks = 10;

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
				_successSound.Play();
				GD.Print("Correct! Recruited one friendly.");
			}
			else
			{
				// Successfully identified foe
				gm.AddScore(20);
				_identifyFoeSound.Play();
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
				LosePackMember();
				_failSound.Play();
				GD.Print("Wrong! Foe pack attacked.");
			}
		}

		// Mark pack as tested (darkens remaining members)
		_currentTestPack.MarkTested();
		_currentTestPack = null;

		// Return to traversal if not game over or won
		if (gm.CurrentState != GameState.GameOver && gm.CurrentState != GameState.GameWon)
		{
			gm.ChangeState(GameState.Traversal);
			// Spawn new packs to maintain minimum
			EnsureMinimumPacks();
		}
	}

	private void RecruitPack(NPCPack pack)
	{
		// Recruit one member with triangle formation
		pack.TransferOneMemberToPlayer(_playerPackContainer, _playerDot);
	}

	private void LosePackMember()
	{
		var gm = GameManager.Instance;
		if (gm == null) return;

		// Game over if player is already alone and trusts a foe
		if (gm.PackSize <= 1)
		{
			gm.ChangeState(GameState.GameOver);
			return;
		}

		var lastMember = gm.GetLastPackMember();
		if (lastMember == null) return;

		gm.RemoveFromPlayerPack(lastMember);
		lastMember.QueueFree();
	}

	private void OnGameStateChanged(int stateInt)
	{
		var newState = (GameState)stateInt;
		switch (newState)
		{
			case GameState.Traversal:
				_traversalMode.ProcessMode = ProcessModeEnum.Inherit;
				_gameOverPanel.Visible = false;
				// Switch to intro/traversal music
				_encounterMusic.Stop();
				if (!_introMusic.Playing)
					_introMusic.Play();
				break;

			case GameState.Testing:
				// Pause traversal while testing
				_traversalMode.ProcessMode = ProcessModeEnum.Disabled;
				// Stop walk sound and switch to encounter music
				_walkSound.Stop();
				_introMusic.Stop();
				_encounterMusic.Play();
				break;

			case GameState.GameOver:
				_traversalMode.ProcessMode = ProcessModeEnum.Disabled;
				_testMode.EndTest();
				_gameOverLabel.Text = "YOU HAVE FAILED!";
				_flavorTextLabel.Text = "You roam the desert alone\nwondering what it might have\nfelt like having friends....";
				_gameOverPanel.Visible = true;
				break;

			case GameState.GameWon:
				_traversalMode.ProcessMode = ProcessModeEnum.Disabled;
				_testMode.EndTest();
				_gameOverLabel.Text = "YOU WIN!";
				_flavorTextLabel.Text = "Your pack roams the desert\ntogether, safe and happy.\nTrue friendship prevails!";
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

	private void OnPlayPressed()
	{
		_startScreen.Visible = false;
		_traversalMode.ProcessMode = ProcessModeEnum.Inherit;
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
