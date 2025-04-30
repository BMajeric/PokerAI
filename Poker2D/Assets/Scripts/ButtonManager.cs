using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private GameObject _startButton;
    [SerializeField] private GameObject _flopButton;
    [SerializeField] private GameObject _turnButton;
    [SerializeField] private GameObject _riverButton;

    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    public void StartButtonHandler()
    {
        _gameManager.StartRound();
        _startButton.SetActive(false);
        _flopButton.SetActive(true);
    }

    public void FlopButtonHandler()
    {
        StartCoroutine(_gameManager.DealFlop());
        _flopButton.SetActive(false);
        _turnButton.SetActive(true);
    }

    public void TurnButtonHandler()
    {
        _gameManager.DealTurn();
        _turnButton.SetActive(false);
        _riverButton.SetActive(true);
    }

    public void RiverButtonHandler()
    {
        _gameManager.DealRiver();
        _riverButton.SetActive(false);
    }
}
