using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Deck _deck;

    void Start()
    {
        _deck = new Deck();

        for (int i = 0; i < 5; i++)
        {
            _deck.DrawCard();
        }
    }

}
