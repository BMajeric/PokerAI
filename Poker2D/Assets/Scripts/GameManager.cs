using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private List<Transform> handCardTransforms;
    [SerializeField] private List<Transform> flopCardTransforms;
    [SerializeField] private Transform turnCardTransform;
    [SerializeField] private Transform riverCardTransform;

    private Deck _deck;
    private Dictionary<string, Sprite> _cardSprites;

    private Player _player;
    private Player _opponent;

    void Start()
    {
        // Load sprites of cards into dictionary
        Dictionary<string, Sprite> _cardSprites = LoadCardSprites();

        _deck = new Deck(_cardSprites);

        // Create player and opponent
        _player = new Player();
        _opponent = new Player();
        
        // Hand dealing
        for (int i = 0; i < handCardTransforms.Count; i++)
        {
            Card drawnCard = _deck.DrawCard();

            // Add card to hand
            if (i % 2 == 0)
            {
                // Deal card to player
                _player.ReceiveCard(drawnCard);
            } 
            else
            {
                // Deal card to opponent
                _opponent.ReceiveCard(drawnCard);
            }

            // Create card game object
            GameObject drawnCardGameObject = Instantiate(cardPrefab, handCardTransforms[i]);
            drawnCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = drawnCard.CardSprite;
            drawnCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
        }

        //// TEST: flop, turn and river draw
        //// Flop
        //for (int i = 0; i < flopCardTransforms.Count; i++)
        //{
        //    Card flopCard = _deck.DrawCard();
        //    GameObject flopCardGameObject = Instantiate(cardPrefab, flopCardTransforms[i]);
        //    flopCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = flopCard.CardSprite;
        //    flopCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
        //}
        //// Turn
        //Card turnCard = _deck.DrawCard();
        //GameObject turnCardGameObject = Instantiate(cardPrefab, turnCardTransform);
        //turnCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = turnCard.CardSprite;
        //turnCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
        //// River
        //Card riverCard = _deck.DrawCard();
        //GameObject riverCardGameObject = Instantiate(cardPrefab, riverCardTransform);
        //riverCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = riverCard.CardSprite;
        //riverCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
    }

    private Dictionary<string, Sprite> LoadCardSprites()
    {
        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("CardSprites");

        foreach(Sprite sprite in loadedSprites)
        {
            sprites[sprite.name] = sprite;
        }

        return sprites;
    }

}
