using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private List<Transform> _handCardTransforms;
    [SerializeField] private List<Transform> _flopCardTransforms;
    [SerializeField] private Transform _turnCardTransform;
    [SerializeField] private Transform _riverCardTransform;

    private Deck _deck;

    private Player _player;
    private Player _opponent;
    private Table _table;

    private GameState _gameState;

    void Start()
    {
        // Load sprites of cards into a dictionary
        Dictionary<string, Sprite> cardSprites = LoadCardSprites("CardSprites");

        // Create the deck
        _deck = new Deck(cardSprites);

        // Create player and opponent
        _player = new Player();
        _opponent = new Player();

        // Create table for community cards
        _table = new Table();

        // StartRound();
    }

    public void StartRound()
    {
        // Set the game state 
        _gameState = GameState.ROUND_START;

        // Deal player and opponent hands
        StartCoroutine(DealHands());
    }

    private IEnumerator DealHands()
    {
        // Hand dealing
        for (int i = 0; i < _handCardTransforms.Count; i++)
        {
            Card drawnCard = _deck.DrawCard();

            // Create card game object
            GameObject drawnCardGameObject = Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);

            // Add card to hand
            if (i % 2 == 0)
            {
                // Deal card to player
                _player.ReceiveCard(drawnCard, drawnCardGameObject, true);
            }
            else
            {
                // Deal card to opponent
                _opponent.ReceiveCard(drawnCard, drawnCardGameObject, false);
            }

            // Animate it to its position in hand
            AnimateCardDraw(drawnCardGameObject.transform, _handCardTransforms[i]);

            yield return new WaitForSeconds(0.25f);
        }
    }

    public void DealFlop()
    {
        StartCoroutine(DealFlopCoroutine());
    }

    public IEnumerator DealFlopCoroutine()
    {
        // Set the game state
        _gameState = GameState.FLOP;

        for (int i = 0; i < _flopCardTransforms.Count; i++)
        {
            // Create card and card game object
            Card flopCard = _deck.DrawCard();
            GameObject flopCardGameObject = Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);

            // Pass card to the table class
            _table.AddCard(flopCard, flopCardGameObject);

            // Animate card dealing
            AnimateCardDraw(flopCardGameObject.transform, _flopCardTransforms[i]);
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void DealTurn()
    {
        // Set the game state
        _gameState = GameState.TURN;

        // Create card and card game object
        Card turnCard = _deck.DrawCard();
        GameObject turnCardGameObject = Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);

        // Pass card to the table class
        _table.AddCard(turnCard, turnCardGameObject);

        // Animate card dealing
        AnimateCardDraw(turnCardGameObject.transform, _turnCardTransform);
    }

    public void DealRiver()
    {
        // Set the game state
        _gameState = GameState.RIVER;

        // Create card and card game object
        Card riverCard = _deck.DrawCard();
        GameObject riverCardGameObject = Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);

        // Pass card to the table class
        _table.AddCard(riverCard, riverCardGameObject);

        // Animate card dealing
        AnimateCardDraw(riverCardGameObject.transform, _riverCardTransform);
    }

    public void CalculatePlayersHandStrength()
    {
        _player.GetHand().CalculateHandStrength(_table.CommunityCards);
    }

    public void ShowOpponentHand()
    {
        StartCoroutine(_opponent.GetHand().RevealHandAnimated());
    }

    public void EndRound()
    {
        // TODO: Remove player and community cards and animate it
    }

    private Dictionary<string, Sprite> LoadCardSprites(string folderName)
    {
        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(folderName);

        foreach(Sprite sprite in loadedSprites)
        {
            sprites[sprite.name] = sprite;
        }

        return sprites;
    }

    private void AnimateCardDraw(Transform cardTransform, Transform goalTransform)
    {
        cardTransform.DOLocalMove(goalTransform.position, 0.5f);
        cardTransform.DOLocalRotate(goalTransform.rotation.eulerAngles, 0.5f);
    }

}
