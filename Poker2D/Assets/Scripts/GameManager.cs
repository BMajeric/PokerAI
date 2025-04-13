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
        //_cardSprites = LoadCardSprites();

        _deck = new Deck();

        for (int i = 0; i < 2; i++)
        {
            _deck.DrawCard();
            //GameObject drawnCard = Instantiate(cardPrefab, handCardTransform[i]);
            //drawnCard.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().sprite = _cardSprites["Ace_of_Spades"];
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
