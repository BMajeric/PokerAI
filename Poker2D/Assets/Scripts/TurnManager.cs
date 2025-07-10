using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    // Managers
    private ButtonManager _buttonManager;

    // Game related data
    private Player _player;
    private OpponentAISimple _opponent;
    private Table _table;

    private GameState _gameState;

    private int _playerPot;
    private int _opponentPot;

    public int Pot => _playerPot + _opponentPot;

    private bool _isPlayersTurn = true;

    private bool _playerPlayed;
    private bool _opponentPlayed;

    // Actions
    public event Action<Player> OnRoundEnded;


    public void InitializeTurnManager(Player player, OpponentAISimple opponent, Table table)
    {
        // Initialize variables passed
        _player = player;
        _opponent = opponent;
        _table = table;
        
        // Initialize managers
        _buttonManager = GameObject.Find("ButtonManager").GetComponent<ButtonManager>();

        // Subscribe to necesarry events
        _buttonManager.OnPlayerFolded += HandlePlayerFolded;
        _buttonManager.OnPlayerChecked += HandlePlayerChecked;
        _buttonManager.OnPlayerCalled += HandlePlayerCalled;
        _buttonManager.OnPlayerRaised += HandlePlayerRaised;
    }

    public void StartTurn()
    {
        if (_isPlayersTurn)
            HandleAction(_player);
        else
            HandleAction(_opponent);
    }

    private void HandleAction(Player actor)
    {
        if (actor == _player)
        {
            _buttonManager.EnablePlayerBettingUI();
            Debug.Log($"Player credit: {_player.Chips}");
        }
        else
        {
            // Give opponent AI the data needed to make a decision
        }
    }

    private void HandlePlayerFolded()
    {
        Debug.Log("Player folded!");
        // Tell opponent AI player folded

        // Add chips from pot to opponent
        _opponent.AddChips(Pot);

        // End round
        OnRoundEnded?.Invoke(_opponent);
    }

    private void HandlePlayerChecked()
    {
        Debug.Log("Player checked!");
        if (_opponentPlayed)
        {
            // Move on to next round state
        }
        else
        {
            // Update last player action

            // Pass turn to opponent
        }
    }

    private void HandlePlayerCalled()
    {
        Debug.Log("Player called!");
        // Update pot
        int betAmount = _opponentPot - _playerPot;
        _player.BetChips(betAmount);
        _playerPot += betAmount;

        // Move on to next round state
    }

    private void HandlePlayerRaised(int amount)
    {
        Debug.Log($"Player raised {amount}!");
        // Update pot
        _player.BetChips(amount);
        _playerPot += amount;

        // Pass turn to opponoent
    }

}
