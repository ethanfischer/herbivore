using Godot;
using System.Collections.Generic;
using Herbivore.Data;
using Herbivore.Traversal;

namespace Herbivore.Autoloads;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!;

    [Signal]
    public delegate void StateChangedEventHandler(int newState);

    [Signal]
    public delegate void PackSizeChangedEventHandler(int newSize);

    [Signal]
    public delegate void ScoreChangedEventHandler(int newScore);

    private GameState _currentState = GameState.Traversal;
    private readonly List<PackMember> _playerPack = new();
    private int _score;

    public GameState CurrentState => _currentState;
    public int PackSize => _playerPack.Count + 1; // +1 for player
    public int Score => _score;
    public IReadOnlyList<PackMember> PlayerPack => _playerPack;
    public bool IsRestart { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void ChangeState(GameState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        EmitSignal(SignalName.StateChanged, (int)newState);

        GD.Print($"Game state changed to: {newState}");
    }

    public void AddToPlayerPack(PackMember member)
    {
        _playerPack.Add(member);
        EmitSignal(SignalName.PackSizeChanged, PackSize);

        // Check for win condition
        if (PackSize >= 10)
        {
            ChangeState(GameState.GameWon);
        }
    }

    public void RemoveFromPlayerPack(PackMember member)
    {
        _playerPack.Remove(member);
        EmitSignal(SignalName.PackSizeChanged, PackSize);
    }

    public PackMember? GetLastPackMember()
    {
        return _playerPack.Count > 0 ? _playerPack[^1] : null;
    }

    public void AddScore(int points)
    {
        _score += points;
        EmitSignal(SignalName.ScoreChanged, _score);
    }

    public int CalculateClicks(int npcPackSize)
    {
        float ratio = PackSize / Mathf.Max(npcPackSize, 1f);
        return Mathf.Clamp(5 + (int)(ratio * 3), 3, 25);
    }

    public void ResetGame()
    {
        _score = 0;
        _playerPack.Clear();
        IsRestart = true;
        ChangeState(GameState.Traversal);
        EmitSignal(SignalName.ScoreChanged, 0);
        EmitSignal(SignalName.PackSizeChanged, 1);
    }
}
