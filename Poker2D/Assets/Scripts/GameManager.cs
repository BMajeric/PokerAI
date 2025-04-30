using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private List<Transform> handCardTransforms;
    [SerializeField] private List<Transform> flopCardTransforms;
    [SerializeField] private Transform turnCardTransform;
    [SerializeField] private Transform riverCardTransform;

    private Deck _deck;
    private Sprite _activeCardBack;

    private Player _player;
    private Player _opponent;

    private GameState _gameState;

    void Start()
    {
        // Load sprites of cards into a dictionary
        Dictionary<string, Sprite> cardSprites = LoadCardSprites("CardSprites");

        // Load sprites of card backs into a dictionary
        Dictionary<string, Sprite> cardBacks = LoadCardSprites("CardBacks");

        // Set default card back
        _activeCardBack = cardBacks["Card_Back"];

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
        for (int i = 0; i < handCardTransforms.Count; i++)
        {
            Card drawnCard = _deck.DrawCard();

            // Create card game object
            GameObject drawnCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);

            // Add card to hand
            if (i % 2 == 0)
            {
                // Deal card to player
                _player.ReceiveCard(drawnCard);
                drawnCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = drawnCard.CardSprite;
            }
            else
            {
                // Deal card to opponent
                _opponent.ReceiveCard(drawnCard);
                drawnCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = _activeCardBack;
            }

            drawnCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

            // Animate it to its position in hand
            AnimateCardDraw(drawnCardGameObject.transform, handCardTransforms[i]);

            yield return new WaitForSeconds(0.25f);
        }
    }

    public IEnumerator DealFlop()
    {
        _gameState = GameState.FLOP;
        for (int i = 0; i < flopCardTransforms.Count; i++)
        {
            Card flopCard = _deck.DrawCard();
            GameObject flopCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
            flopCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = flopCard.CardSprite;
            flopCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

            AnimateCardDraw(flopCardGameObject.transform, flopCardTransforms[i]);
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void DealTurn()
    {
        _gameState = GameState.TURN;
        Card turnCard = _deck.DrawCard();
        GameObject turnCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        turnCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = turnCard.CardSprite;
        turnCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

        AnimateCardDraw(turnCardGameObject.transform, turnCardTransform);
    }

    public void DealRiver()
    {
        _gameState = GameState.RIVER;
        Card riverCard = _deck.DrawCard();
        GameObject riverCardGameObject = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        riverCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = riverCard.CardSprite;
        riverCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

        AnimateCardDraw(riverCardGameObject.transform, riverCardTransform);
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
