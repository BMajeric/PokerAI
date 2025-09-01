using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private Button _startButton;

    [SerializeField] private Button _foldButton;
    [SerializeField] private Button _checkButton;
    [SerializeField] private Button _callButton;
    [SerializeField] private Button _raiseButton;
    [SerializeField] private TMP_InputField _bettingInputField;
    [SerializeField] private Slider _bettingSlider;

    private GameManager _gameManager;
    private TurnManager _turnManager;

    private Player _player = null;
    private Player _opponent = null;

    private bool _isUpdatingFromSlider = false;
    private bool _isUpdatingFromInput = false;

    public event Action OnPlayerFolded;
    public event Action OnPlayerChecked;
    public event Action OnPlayerCalled;
    public event Action<int> OnPlayerRaised;

    private void Awake()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _turnManager = GameObject.Find("TurnManager").GetComponent<TurnManager>();
    }

    private void Start()
    {
        // Initialize the input field with the slider's value
        UpdateBettingInputField((int)_bettingSlider.value);

        // Subscribe functions to UI element events
        _foldButton.onClick.AddListener(FoldButtonHandler);
        _callButton.onClick.AddListener(CallButtonHandler);
        _checkButton.onClick.AddListener(CheckButtonHandler);
        _raiseButton.onClick.AddListener(RaiseButtonHandler);
        _bettingSlider.onValueChanged.AddListener(OnBettingSliderValueChanged);
        _bettingInputField.onValueChanged.AddListener(OnBettingInputFieldValueChanged);
    }

    public void GivePlayerInfo(Player player, Player opponent)
    {
        _player = player;
        _opponent = opponent;
    }

    public void EnablePlayerBettingUI(bool canCheck, int playerBettingSliderMinValue)
    {
        if (_player == null || _opponent == null)
        {
            Debug.Log("Players not initialized in Button manager!!!");
            return;
        }

        // Update betting slider min and max values
        _bettingSlider.minValue = playerBettingSliderMinValue;
        _bettingSlider.maxValue = _player.Chips;
        _bettingSlider.value = playerBettingSliderMinValue;


        Debug.Log($"Player chips for betting: {_player.Chips}");

        _foldButton.gameObject.SetActive(true);
        _raiseButton.gameObject.SetActive(true);
        _bettingInputField.gameObject.SetActive(true);
        _bettingSlider.gameObject.SetActive(true);
        
        if (canCheck)
            _checkButton.gameObject.SetActive(true);
        else
            _callButton.gameObject.SetActive(true);
    }

    public void DisablePlayerBettingUI()
    {
        _foldButton.gameObject.SetActive(false);
        _checkButton.gameObject.SetActive(false);
        _callButton.gameObject.SetActive(false);
        _raiseButton.gameObject.SetActive(false);
        _bettingInputField.gameObject.SetActive(false);
        _bettingSlider.gameObject.SetActive(false);
    }

    public void StartButtonHandler()
    {
        _gameManager.StartRound();
        _startButton.gameObject.SetActive(false);
    }

    public void FoldButtonHandler()
    {
        // Disable betting UI
        DisablePlayerBettingUI();

        // Signal player fold
        OnPlayerFolded?.Invoke();
    }
    
    public void CheckButtonHandler()
    {
        // Disable betting UI
        DisablePlayerBettingUI();
        
        // Signal player check
        OnPlayerChecked?.Invoke();
    }

    public void CallButtonHandler()
    {
        // Disable betting UI
        DisablePlayerBettingUI();
        
        // Signal player call
        OnPlayerCalled?.Invoke();
    }

    public void RaiseButtonHandler()
    {
        // Disable betting UI
        DisablePlayerBettingUI();
        
        // Signal player raise with amount raised
        OnPlayerRaised?.Invoke((int)_bettingSlider.value);
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
        // Needed to move the line that should blink at the end of text to end of text (possible bug due to the "$" sign)
        yield return null; // Wait 1 frame
        _bettingInputField.caretPosition = _bettingInputField.text.Length;
    }

}
