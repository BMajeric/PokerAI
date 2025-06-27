using UnityEngine;

public class TurnManager
{
    private GameManager _gameManager;
    private Player _player;
    private OpponentAISimple _opponent;
    private Table _table;

    public int Pot;
    
    private int _playerPot;
    private int _opponentPot;

    private bool _isPlayersTurn;

    private bool _playerPlayed;
    private bool _opponentPlayed;

    public TurnManager(GameManager gm, Player player, OpponentAISimple opponent, Table table)
    {
        _gameManager = gm;
        _player = player;
        _opponent = opponent;
        _table = table;
    }

    private void StartTurn()
    {
        if (_isPlayersTurn)
            HandleAction(_player);
        else
            HandleAction(_opponent);
    }

    private void HandleAction(Player actor)
    {

    }

}
