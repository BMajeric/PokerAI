using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private GameObject _startButton;
    [SerializeField] private GameObject _flopButton;
    [SerializeField] private GameObject _turnButton;
    [SerializeField] private GameObject _riverButton;
    [SerializeField] private GameObject _nextRoundButton;
    [SerializeField] private TMP_InputField _bettingInputField;
    [SerializeField] private Slider _bettingSlider;

    private GameManager _gameManager;

    private bool _isUpdatingFromSlider = false;
    private bool _isUpdatingFromInput = false;

    private void Awake()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    private void Start()
    {
        // Initialize the input field with the slider's value
        UpdateBettingInputField((int)_bettingSlider.value);

        _bettingSlider.onValueChanged.AddListener(OnBettingSliderValueChanged);

        _bettingInputField.onValueChanged.AddListener(OnBettingInputFieldValueChanged);
    }

    public void StartButtonHandler()
    {
        _gameManager.StartRound();
        _startButton.SetActive(false);
        _flopButton.SetActive(true);
    }

    public void FlopButtonHandler()
    {
        _gameManager.DealFlop();
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
        _nextRoundButton.SetActive(true);
        _gameManager.ShowOpponentHand();
        _gameManager.ComparePlayersHandStrength();
    }

    public void NextRoundButtonHandler()
    {
        _gameManager.EndRound();
        _gameManager.StartRound();
        _nextRoundButton.SetActive(false);
        _flopButton.SetActive(true);
    }

    public void FoldButtonHandler()
    {
        // Start game manager function to signal fold (animating card push)

        // Signal to opponent that they win

        // Go to start of the next round

    }
    
    public void CheckButtonHandler()
    {
        // Go to next game state

    }

    public void CallButtonHandler()
    {
        // Add money to the player's pot and game pot

        // Go to next game state

    }

    public void RaiseButtonHandler()
    {
        // Check if slider and input field value is > 0, return if not

        // Add the amount of chips set in the slider and input field to the player's pot and game pot

        // Hand control over to the opponent

    }

    private void OnBettingSliderValueChanged(float value)
    {
        if (_isUpdatingFromInput) return;

        _isUpdatingFromSlider = true;
        UpdateBettingInputField((int)value);
        _isUpdatingFromSlider = false;
    }

    private void UpdateBettingInputField(int value)
    {
        _bettingInputField.text = $"${value}";
        StartCoroutine(SetCaretNextFrameCoroutine());
    }

    private void OnBettingInputFieldValueChanged(string input)
    {
        if (_isUpdatingFromSlider) return;

        _isUpdatingFromInput = true;

        string sanitizedInput = input.Replace("$", "").Trim();

        if (int.TryParse(sanitizedInput, out int parsedValue))
        {
            parsedValue = Mathf.Clamp(parsedValue, (int)_bettingSlider.minValue, (int)_bettingSlider.maxValue);
            _bettingSlider.value = parsedValue;
            _bettingInputField.text = $"${parsedValue}";
            Debug.Log(_bettingInputField.text.Length);

            StartCoroutine(SetCaretNextFrameCoroutine());
        } 
        else if (string.IsNullOrEmpty(sanitizedInput))
        {
            _bettingInputField.text = "$";
            _bettingSlider.value = _bettingSlider.minValue;
            StartCoroutine(SetCaretNextFrameCoroutine());
        }

        _isUpdatingFromInput = false;
    }

    private IEnumerator SetCaretNextFrameCoroutine()
    {
        yield return null; // Wait 1 frame
        _bettingInputField.caretPosition = _bettingInputField.text.Length;
    }

}
