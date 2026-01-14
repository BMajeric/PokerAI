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
    public GameState CurrentGameState => _gameState;

    private bool _isPlayersTurn;
    private bool _isPlayersTurnOnRoundStart;

    private bool _playerPlayed;
    private bool _opponentPlayed;

    // Events
    public event Action<Player> OnRoundEnded;
    public event Action<GameState> OnGameStateChanged;
    public event Action<Player> OnWinnerDetermined;
    public event Action<int> OnPlayerChipsChange;
    public event Action<int> OnOpponentChipsChange;
    public event Action<int> OnPotValueChanged;

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

    public void StartRound(bool isPlayersTurn, int smallBlind, int bigBlind)
    {
        _isPlayersTurn = isPlayersTurn;
        _isPlayersTurnOnRoundStart = isPlayersTurn;

        // Handle blinds
        _playerPot = isPlayersTurn ? smallBlind : bigBlind;
        _player.BetChips(_playerPot);

        _opponentPot = isPlayersTurn ? bigBlind : smallBlind;
        _opponent.BetChips(_opponentPot);

        // Update UI
        OnPlayerChipsChange?.Invoke(_player.Chips);
        OnOpponentChipsChange?.Invoke(_opponent.Chips);
        OnPotValueChanged?.Invoke(Pot);

        // Set game state
        _gameState = GameState.PRE_FLOP;

        StartTurn();
    }

    public void StartTurn()
    {
        Debug.Log($"Is it players turn to act on turn start? {_isPlayersTurn}");
        if (_isPlayersTurn)
            _buttonManager.EnablePlayerBettingUI(
                _opponentPot > _playerPot ? false : true, 
                _opponentPot - _playerPot);
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

            // Update UI
            OnPlayerChipsChange?.Invoke(_player.Chips);
            OnPotValueChanged?.Invoke(Pot);
        }
        else
        {
            _opponentPlayed = true;
            _opponent.BetChips(amount);
            _opponentPot += amount;

            // Update UI
            OnOpponentChipsChange?.Invoke(_opponent.Chips);
            OnPotValueChanged?.Invoke(Pot);
        }

        // Handle logic for folding
        if (action == PlayerAction.FOLD)
        {
            Player winner = _isPlayersTurn ? _opponent : _player;
            StartCoroutine(EndRoundWithWinnerCoroutine(winner));
            // Debug.Log($"OPPONENT CHIPS NOW: {_opponent.Chips}");
            return;
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

        // Reset who has played their turn
        _playerPlayed = false;
        _opponentPlayed = false;

        // Notify others that the game state changed
        OnGameStateChanged?.Invoke(_gameState);

        // Pass the turn to the player that starts it this round
        Debug.Log($"Game State: {_gameState}");
        if (_gameState != GameState.SHOWDOWN && _gameState != GameState.ROUND_END)
            _isPlayersTurn = _isPlayersTurnOnRoundStart;
        else
            return;     // Don't go to start turn again if the turns are over

        // Start next phase
        StartTurn();
    }

    private void AskOpponentForDecision()
    {
        StartCoroutine(AskOpponentForDecisionCoroutine());
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
        StartCoroutine(EndRoundWithWinnerCoroutine(winner));
    }

    private IEnumerator EndRoundWithWinnerCoroutine(Player winner)
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

        // Reset the pots
        _playerPot = 0;
        _opponentPot = 0;

        // Update chips and pot UI
        OnPlayerChipsChange?.Invoke(_player.Chips);
        OnOpponentChipsChange?.Invoke(_opponent.Chips);
        OnPotValueChanged?.Invoke(Pot);

        // Add wait to give player a chance to see the enemy cards
        yield return new WaitForSeconds(8);

        // PLACEHOLDER: refill the player or opponent chips if they lost all
        if (_player.Chips <= 0)
        {
            _player.AddChips(0 - _player.Chips + 2500);     // TODO: make sure player cannot have less than 0 chips
        }
        if (_opponent.Chips <= 0)
        {
            _opponent.AddChips(0 - _opponent.Chips + 2500);
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
            PlayerChips = _player.Chips,
            OpponentChips = _opponent.Chips,
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
        OnWinnerDetermined -= HandleShowdownConcluded;
    }

}
