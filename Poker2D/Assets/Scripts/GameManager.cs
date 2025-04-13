using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private List<Transform> handCardTransform;

    private Deck _deck;
    private Dictionary<string, Sprite> _cardSprites;

    void Start()
    {
        // Load sprites of cards into dictionary
        Dictionary<string, Sprite> _cardSprites = LoadCardSprites();

        _deck = new Deck(_cardSprites);

        for (int i = 0; i < 2; i++)
        {
            Card drawnCard = _deck.DrawCard();
            GameObject drawnCardGameObject = Instantiate(cardPrefab, handCardTransform[i]);
            drawnCardGameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = drawnCard.CardSprite;
            drawnCardGameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;
        }
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
