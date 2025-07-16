using UnityEngine;
using System;
using System.Collections;

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

    private bool _isPlayersTurn;
    private bool _isPlayersTurnOnRoundStart;

    private bool _playerPlayed;
    private bool _opponentPlayed;

    // Events
    public event Action<Player> OnRoundEnded;
    public event Action<GameState> OnGameStateChanged;
    public event Action<Player> OnWinnerDetermined;

    private void Awake()
    {
        // Initialize managers
        _buttonManager = GameObject.Find("ButtonManager").GetComponent<ButtonManager>();
    }

    private void Start()
    {
        // Subscribe to necessary events
        _buttonManager.OnPlayerFolded += HandlePlayerFolded;
        _buttonManager.OnPlayerChecked += HandlePlayerChecked;
        _buttonManager.OnPlayerCalled += HandlePlayerCalled;
        _buttonManager.OnPlayerRaised += HandlePlayerRaised;
        OnWinnerDetermined += HandleShowdownConcluded;
    }


    public void InitializeTurnManager(Player player, OpponentAISimple opponent, Table table)
    {
        // Initialize variables passed
        _player = player;
        _opponent = opponent;
        _table = table;
    }

    public void StartRound(bool isPlayersTurn, int playerPot, int opponentPot)
    {
        _isPlayersTurn = isPlayersTurn;
        _isPlayersTurnOnRoundStart = isPlayersTurn;
        _playerPot = playerPot;
        _opponentPot = opponentPot;

        _gameState = GameState.PRE_FLOP;

        StartTurn();
    }

    public void StartTurn()
    {
        Debug.Log($"Is it players turn to act on turn start? {_isPlayersTurn}");
        if (_isPlayersTurn)
            _buttonManager.EnablePlayerBettingUI();
        else
            AskOpponentForDecision();
    }

    private void HandleAction(PlayerAction action, int amount)
    {
        Debug.Log($"Is it players turn to act? {_isPlayersTurn}");
        if (_isPlayersTurn)
        {
            _playerPlayed = true;
            _player.BetChips(amount);
            _playerPot += amount;
        }
        else
        {
            _opponentPlayed = true;
            _opponent.BetChips(amount);
            _opponentPot += amount;
        }

        // Handle logic for folding
        if (action == PlayerAction.FOLD)
        {
            Player winner = _isPlayersTurn ? _opponent : _player;
            EndRoundWithWinner(winner);
            // Debug.Log($"OPPONENT CHIPS NOW: {_opponent.Chips}");
        }

        // Proceed to next round after both players acted and pot amounts are matched
        if (_playerPlayed && _opponentPlayed && _playerPot == _opponentPot)
        {
            ProceedToNextPhase();
            return;
        }

        // Otherwise, pass the turn to the next player
        _isPlayersTurn = !_isPlayersTurn;
        StartTurn();
    }

    private void ProceedToNextPhase()
    {
        // Switch game state
        if (_gameState == GameState.PRE_FLOP)
        {
            _gameState = GameState.FLOP;
        }
        else if (_gameState == GameState.FLOP)
        {
            _gameState = GameState.TURN;
        }
        else if (_gameState == GameState.TURN)
        {
            _gameState = GameState.RIVER;
        }
        else if (_gameState == GameState.RIVER)
        {
            _gameState = GameState.SHOWDOWN;
        }

        // Notify others that the game state changed
        OnGameStateChanged?.Invoke(_gameState);

        // Start next phase
        _isPlayersTurn = _isPlayersTurnOnRoundStart;
        StartTurn();
    }

    private void AskOpponentForDecision()
    {
        StartCoroutine("AskOpponentForDecisionCoroutine");
    }

    private IEnumerator AskOpponentForDecisionCoroutine()
    {
        // Make a pause to simulate thinking
        float thinkingTime = UnityEngine.Random.Range(0, 7);
        yield return new WaitForSeconds(thinkingTime);

        (PlayerAction action, int amount) decision = _opponent.MakeDecision(GetGameState());

        Debug.Log($"Opponent decided to {decision.action}, amount: {decision.amount}");

        // Tell the turn manager what the opponent decided
        HandleAction(decision.action, decision.amount);
    }

    private void HandleShowdownConcluded(Player winner)
    {
        EndRoundWithWinner(winner);
    }

    private void EndRoundWithWinner(Player winner)
    {
        // Distribute chips
        if (winner == null)
        {
            _player.AddChips(_playerPot);
            _opponent.AddChips(_opponentPot);
        }
        else
        {
            winner.AddChips(Pot);
        }

        // Notify that the round has ended
        OnRoundEnded?.Invoke(winner);
    }

    private void HandlePlayerFolded()
    {
        Debug.Log("Player folded!");

        HandleAction(PlayerAction.FOLD, 0);
    }

    private void HandlePlayerChecked()
    {
        Debug.Log("Player checked!");

        HandleAction(PlayerAction.CHECK, 0);
    }

    private void HandlePlayerCalled()
    {
        Debug.Log("Player called!");

        HandleAction(PlayerAction.CALL, _opponentPot - _playerPot);
    }

    private void HandlePlayerRaised(int amount)
    {
        Debug.Log($"Player raised {amount}!");

        HandleAction(PlayerAction.RAISE, amount);
    }

    // Helper function so that the game manager can notify the turn manager about the winner of the showdown
    // without the turn manager needing to know about the game manager to avoid circular dependencies
    public void NotifyWinner(Player winner)
    {
        OnWinnerDetermined?.Invoke(winner);
    }

    private GameStateSnapshot GetGameState()
    {
        return new GameStateSnapshot
        {
            PlayerPot = _playerPot,
            OpponentPot = _opponentPot,
            CommunityCards = _table.CommunityCards
        };
    }

    private void OnDestroy()
    {
        // Unsubscribe from all events
        _buttonManager.OnPlayerFolded -= HandlePlayerFolded;
        _buttonManager.OnPlayerChecked -= HandlePlayerChecked;
        _buttonManager.OnPlayerCalled -= HandlePlayerCalled;
        _buttonManager.OnPlayerRaised -= HandlePlayerRaised;
    }

}
