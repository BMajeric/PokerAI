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
        _gameState = GameState.FLOP;
        for (int i = 0; i < _flopCardTransforms.Count; i++)
        {
            Card flopCard = _deck.DrawCard();
            GameObject flopCardGameObject = Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);
            flopCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = flopCard.CardSprite;
            flopCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

            AnimateCardDraw(flopCardGameObject.transform, _flopCardTransforms[i]);
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void DealTurn()
    {
        _gameState = GameState.TURN;
        Card turnCard = _deck.DrawCard();
        GameObject turnCardGameObject = Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);
        turnCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = turnCard.CardSprite;
        turnCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

        AnimateCardDraw(turnCardGameObject.transform, _turnCardTransform);
    }

    public void DealRiver()
    {
        _gameState = GameState.RIVER;
        Card riverCard = _deck.DrawCard();
        GameObject riverCardGameObject = Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);
        riverCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = riverCard.CardSprite;
        riverCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

        AnimateCardDraw(riverCardGameObject.transform, _riverCardTransform);
    }

    public void ShowOpponentHand()
    {
        StartCoroutine(_opponent.GetHand().RevealHandAnimated());
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
