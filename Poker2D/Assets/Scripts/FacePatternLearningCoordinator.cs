using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FacePatternLearningCoordinator : MonoBehaviour
{
    public struct ObservationContext
    {
        public float[] FeatureVector;
        public float Timestamp;
        public bool HasData;
    }

    [Header("Scene References")]
    [SerializeField] private Tracker _faceTracker;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private ButtonManager _buttonManager;

    [Header("Capture Window")]
    [SerializeField] private float _dealPreEventSeconds = 0.1f;
    [SerializeField] private float _dealPostEventSeconds = 1.5f;
    [SerializeField] private float _actionPreEventSeconds = 1.5f;
    [SerializeField] private float _actionPostEventSeconds = 0.25f;

    [Header("Feature Extraction")]
    [SerializeField] private int _fallbackReferenceFeaturePointIndex = 0;

    [Header("Pattern Matching")]
    [SerializeField] private float _patternMatchThreshold = 55f;
    [SerializeField] private bool _enableAutoThresholdCalibration = false;
    [SerializeField] private float _autoThresholdStdMultiplier = 2f;
    [SerializeField] private int _minAutoCalibrationSamples = 10;

    [Header("Bluff Detection")]
    [SerializeField] private HandRanking _preflopStrongThreshold = HandRanking.PAIR;
    [SerializeField] private HandRanking _flopStrongThreshold = HandRanking.PAIR;
    [SerializeField] private HandRanking _turnStrongThreshold = HandRanking.TWO_PAIR;
    [SerializeField] private HandRanking _riverStrongThreshold = HandRanking.TWO_PAIR;
    [SerializeField] private int _premiumPairRankThreshold = 11;    // Premium pair = a pair of Jacks, Queens, Kings or Aces

    [Header("Confidence calculation")]
    [SerializeField] private float _confidentSampleCount = 10f;

    private FaceFrameBuffer _faceFrameBuffer;
    private FeatureVectorExtractor _featureVectorExtractor;
    private PatternManager _patternManager;
    private bool _hasLoadedPatterns;

    private readonly List<Observation> _pendingObservations = new List<Observation>();
    private readonly Dictionary<(GameState, ObservationType), ObservationContext> _recentObservations =
        new Dictionary<(GameState, ObservationType), ObservationContext>();

    public enum ObservationType
    {
        HoleCards,
        Flop,
        Turn,
        River,
        PlayerFold,
        PlayerCheck,
        PlayerCall,
        PlayerRaise
    }

    private class Observation
    {
        public ObservationType Type;
        public float Timestamp;
        public float[] FeatureVector;
        public GameState Stage;
    }

    private struct HandStrengthSnapshot
    {
        public HandRanking Ranking;
        public uint EncodedValue;
        public int PairRank;
    }

    private void Start()
    {
        int referenceIndex = _fallbackReferenceFeaturePointIndex;
        if (_faceTracker != null)
        {
            referenceIndex = _faceTracker.ReferenceFeaturePointIndex;
            _faceFrameBuffer = _faceTracker.FaceFrameBuffer;
        }

        _featureVectorExtractor = new FeatureVectorExtractor(referenceIndex);

        ResetPatternManager();

        Debug.Log($"FacePatternLearningCoordinator: pattern threshold set to {_patternManager.threshold:F4} (autoCalibrate={_patternManager.autoCalibrateThreshold}).");

        RegisterEventHooks();
    }

    private void RegisterEventHooks()
    {
        if (_gameManager != null)
        {
            _gameManager.OnHoleCardsDealt += HandleHoleCardsDealt;
            _gameManager.OnFlopDealt += HandleFlopDealt;
            _gameManager.OnTurnDealt += HandleTurnDealt;
            _gameManager.OnRiverDealt += HandleRiverDealt;
        }

        if (_buttonManager != null)
        {
            _buttonManager.OnPlayerFolded += HandlePlayerFolded;
            _buttonManager.OnPlayerChecked += HandlePlayerChecked;
            _buttonManager.OnPlayerCalled += HandlePlayerCalled;
            _buttonManager.OnPlayerRaised += HandlePlayerRaised;
            _buttonManager.OnLoadExistingPatternsSelected += HandleLoadExistingPatternsSelected;
        }

        if (_turnManager != null)
        {
            _turnManager.OnWinnerDetermined += HandleShowdownResolved;
            _turnManager.OnRoundEnded += HandleRoundEnded;
        }
    }

    private void HandleHoleCardsDealt()
    {
        CaptureObservation(ObservationType.HoleCards);
    }

    private void HandleFlopDealt()
    {
        CaptureObservation(ObservationType.Flop);
    }

    private void HandleTurnDealt()
    {
        CaptureObservation(ObservationType.Turn);
    }

    private void HandleRiverDealt()
    {
        CaptureObservation(ObservationType.River);
    }

    private void HandlePlayerFolded()
    {
        CaptureObservation(ObservationType.PlayerFold);
    }

    private void HandlePlayerChecked()
    {
        CaptureObservation(ObservationType.PlayerCheck);
    }

    private void HandlePlayerCalled()
    {
        CaptureObservation(ObservationType.PlayerCall);
    }

    private void HandlePlayerRaised(int amount)
    {
        CaptureObservation(ObservationType.PlayerRaise);
    }

    private void HandleShowdownResolved(Player winner)
    {
        ResolvePendingObservations(winner);

        bool playerStrong = IsPlayerStrongForStage(_turnManager.CurrentGameState);
        _turnManager.RecordShowdownResult(playerStrong);
    }

    private void HandleRoundEnded(Player winner)
    {
        _pendingObservations.Clear();
        _recentObservations.Clear();

        // Save patterns after every round to keep a consistent save file
        SavePatterns();
    }

    private void HandleLoadExistingPatternsSelected(bool shouldLoad)
    {
        // If patterns are already loaded skip this
        if (_hasLoadedPatterns)
        {
            return;
        }

        // If patterns should be loaded, load them and update the hasLoaded flag
        if (shouldLoad)
        {
            LoadPatterns();
        }

        _hasLoadedPatterns = true;
    }

    private void CaptureObservation(ObservationType observationType)
    {
        // Safety net if the frames are not captured or something malfunctioned with the tracker
        if (_faceFrameBuffer == null)
        {
            Debug.LogWarning("FacePatternLearningCoordinator: FaceFrameBuffer is not available.");
            return;
        }

        StartCoroutine(CaptureObservationCoroutine(observationType));
    }

    private IEnumerator CaptureObservationCoroutine(ObservationType observationType)
    {
        float eventTime = Time.time;

        // Get the time of face capturing before and after event
        //      - different whether the event is a dealing or a player action
        (float preEventSeconds, float postEventSeconds) = GetCaptureWindowSeconds(observationType);

        // Calculate start and end times for the event
        float startTime = eventTime - preEventSeconds;
        float endTime = eventTime + postEventSeconds;

        GameState stage = ResolveStageForObservation(observationType);

        // Wait until the needed time has elapsed to be able to capture those frames from the frame buffer
        // Essentially just "starts capturing frames" from the time of the event to postEventSeconds
        if (postEventSeconds > 0f)
        {
            yield return new WaitForSeconds(postEventSeconds);
        }

        List<FaceFrame> frames = _faceFrameBuffer.GetFramesInRange(startTime, endTime);
        float[] featureVector = _featureVectorExtractor.Extract(frames);

        // Store the observation
        Observation observation = new Observation
        {
            Type = observationType,
            Timestamp = eventTime,
            FeatureVector = featureVector,
            Stage = stage
        };

        _pendingObservations.Add(observation);
        _recentObservations[(stage, observationType)] = new ObservationContext
        {
            FeatureVector = featureVector,
            Timestamp = eventTime,
            HasData = true
        };
    }

    private void ResolvePendingObservations(Player winner)
    {
        if (_gameManager == null || _gameManager.Player == null)
        {
            _pendingObservations.Clear();
            return;
        }

        bool didPlayerWin = winner != null && winner == _gameManager.Player;

        foreach (Observation observation in _pendingObservations)
        {
            // Get the current and previous hand strenth to calculate if player's hand got stronger
            HandStrengthSnapshot observedStrength = EvaluateHandStrengthAtStage(observation.Stage);
            HandStrengthSnapshot previousStrength = EvaluateHandStrengthAtStage(GetPreviousStage(observation.Stage));
            bool improvedSincePrevious = HasImprovedStrength(observedStrength, previousStrength);

            // Decide if player's action was agressive or not and if it was a bluff
            bool isAggressive = IsAggressiveAction(observation.Type);
            bool isStrongHand = IsStrongHandAtStage(observation.Stage, observedStrength) ||
                                (improvedSincePrevious && isAggressive);
            bool wasBluff = !isStrongHand && isAggressive;

            // Find if a pattern like this exists or not and create or update it
            Pattern pattern = _patternManager.FindOrCreate(observation.FeatureVector, out bool isNew, out float distance);
            Debug.Log($"FacePatternLearningCoordinator: match distance={distance:F4} threshold={_patternManager.threshold:F4} new={isNew} patternId={pattern.id}.");
            
            // Create makes a pattern that doesn't have a count so we have to update it
            pattern.Update(observation.FeatureVector, isStrongHand, isAggressive, didPlayerWin);
        }

        Debug.Log($"Coordinator: Different patterns: {_patternManager.patterns.Count}");

        _pendingObservations.Clear();
    }

    public PlayerTendency GetPlayerTendency(GameState stage, ObservationType actionType)
    {
        // Early exits if things are not configured as they should be
        if (_patternManager == null)
        {
            return PlayerTendency.None;
        }

        if (!_recentObservations.TryGetValue((stage, actionType), out ObservationContext context))
        {
            return PlayerTendency.None;
        }

        if (!context.HasData || context.FeatureVector == null)
        {
            return PlayerTendency.None;
        }

        if (!_patternManager.TryGetClosestPattern(context.FeatureVector, out Pattern pattern, out float distance))
        {
            return PlayerTendency.None;
        }

        // Calculate pattern confidence by linearly combining:
        // Distance factor:
        //          - clamps distance so it doesn't baloon above 1
        //          - reverses it so 1 is a perfect match and 0 is a match at the threshold
        // Sample factor:
        //          - uses the estimated amount of samples needed to calssify it as confident
        //          - clamps percent of confident sample amount between 0 and 1
        float distanceFactor = 1f - Mathf.Clamp01(distance / Mathf.Max(_patternManager.threshold, 0.0001f));
        float sampleFactor = Mathf.Clamp01(pattern.count / _confidentSampleCount);
        float confidence = Mathf.Clamp01(distanceFactor * 0.5f + sampleFactor * 0.5f);

        return PlayerTendency.FromPattern(pattern, confidence);
    }

    public void SavePatterns()
    {
        if (_patternManager == null)
        {
            return;
        }

        // Check if feature vector is available
        int featureVectorLength = ResolveExpectedFeatureVectorLength();
        if (featureVectorLength <= 0)
        {
            Debug.LogWarning("FacePatternLearningCoordinator: skipped saving patterns because feature vector length is unavailable.");
            return;
        }

        // Convert the pattern manager to Data transfer object
        PatternManagerDto dto = _patternManager.ToDto(featureVectorLength);

        // Convert to JSON and save the file to dedicated path
        string json = JsonUtility.ToJson(dto, true);
        string path = GetPatternFilePath();

        try
        {
            File.WriteAllText(path, json);
        }
        catch (IOException exception)
        {
            Debug.LogWarning($"FacePatternLearningCoordinator: failed to save patterns to {path}. {exception.Message}");
        }
    }

    public void LoadPatterns()
    {
        // Create pattern manager if it doesn't exist
        if (_patternManager == null)
        {
            ResetPatternManager();
        }

        // Read the file from the dedicated path
        string path = GetPatternFilePath();
        if (!File.Exists(path))
        {
            ResetPatternManager();
            Debug.Log($"FacePatternLearningCoordinator: no saved patterns found at {path}.");
            return;
        }

        // Parse the JSON file to pattern manager object
        try
        {
            string json = File.ReadAllText(path);
            PatternManagerDto dto = JsonUtility.FromJson<PatternManagerDto>(json);

            // Don't load data that would not be compatible with the new coming data
            if (!IsPatternDataCompatible(dto))
            {
                ResetPatternManager();
                Debug.LogWarning("FacePatternLearningCoordinator: saved patterns were incompatible. Resetting pattern memory.");
                return;
            }

            _patternManager.LoadFromDto(dto);
            Debug.Log($"FacePatternLearningCoordinator: loaded {_patternManager.patterns.Count} patterns from {path}.");
        }
        catch (IOException exception)
        {
            ResetPatternManager();
            Debug.LogWarning($"FacePatternLearningCoordinator: failed to load patterns from {path}. {exception.Message}");
        }
    }


    private GameState ResolveStageForObservation(ObservationType observationType)
    {
        switch (observationType)
        {
            case ObservationType.HoleCards:
                return GameState.PRE_FLOP;
            case ObservationType.Flop:
                return GameState.FLOP;
            case ObservationType.Turn:
                return GameState.TURN;
            case ObservationType.River:
                return GameState.RIVER;
            default:
                return _turnManager != null ? _turnManager.CurrentGameState : GameState.PRE_FLOP;
        }
    }

    private HandStrengthSnapshot EvaluateHandStrengthAtStage(GameState stage)
    {
        if (_gameManager == null || _gameManager.Player == null)
        {
            return new HandStrengthSnapshot
            {
                Ranking = HandRanking.HIGH_CARD,
                EncodedValue = 0,
                PairRank = 0
            };
        }

        // Calculate how many community cards are available at that stage of the game
        int communityCardCount = GetCommunityCardCountForStage(stage);
        List<Card> availableCommunityCards = _gameManager.CommunityCards;
        int cardsToTake = Mathf.Min(communityCardCount, availableCommunityCards.Count);

        // List with all the cards available to the player at that stage (hand + community)
        List<Card> cards = new List<Card>(_gameManager.Player.GetHand().Cards);
        for (int i = 0; i < cardsToTake; i++)
        {
            cards.Add(availableCommunityCards[i]);
        }

        // Calculate player hand strength at that stage
        (HandRanking ranking, uint encodedValue) result = HandEvaluator.CalculateHandStrength(cards);
        int pairRank = result.ranking == HandRanking.PAIR ? GetPairRankFromEncodedValue(result.encodedValue) : 0;

        // Return the snapshot of the player hand strength
        return new HandStrengthSnapshot
        {
            Ranking = result.ranking,
            EncodedValue = result.encodedValue,
            PairRank = pairRank
        };
    }

    public bool IsPlayerStrongForStage(GameState stage)
    {
        HandStrengthSnapshot strength = EvaluateHandStrengthAtStage(stage);
        return IsStrongHandAtStage(stage, strength);
    }

    private int GetCommunityCardCountForStage(GameState stage)
    {
        switch (stage)
        {
            case GameState.PRE_FLOP:
                return 0;
            case GameState.FLOP:
                return 3;
            case GameState.TURN:
                return 4;
            case GameState.RIVER:
            case GameState.SHOWDOWN:
            case GameState.ROUND_END:
                return 5;
            default:
                return 5;
        }
    }

    private GameState GetPreviousStage(GameState stage)
    {
        switch (stage)
        {
            case GameState.FLOP:
                return GameState.PRE_FLOP;
            case GameState.TURN:
                return GameState.FLOP;
            case GameState.RIVER:
                return GameState.TURN;
            default:
                return stage;
        }
    }

    private bool HasImprovedStrength(HandStrengthSnapshot current, HandStrengthSnapshot previous)
    {
        if (current.Ranking > previous.Ranking)
        {
            return true;
        }

        return current.Ranking == previous.Ranking && current.EncodedValue > previous.EncodedValue;
    }

    private bool IsAggressiveAction(ObservationType observationType)
    {
        switch (observationType)
        {
            case ObservationType.PlayerRaise:
            case ObservationType.PlayerCall:
                return true;
            default:
                return false;
        }
    }

    private bool IsStrongHandAtStage(GameState stage, HandStrengthSnapshot strength)
    {
        HandRanking threshold = GetStrongThresholdForStage(stage);
        if (strength.Ranking > threshold)
        {
            return true;
        }

        if (strength.Ranking < threshold)
        {
            // To make sure that a premium pair is always returned as strong in the pre-flop and flop,
            // even if threshold is changed
            return IsPremiumPairAtEarlyStage(stage, strength);
        }

        return true;
    }

    private HandRanking GetStrongThresholdForStage(GameState stage)
    {
        switch (stage)
        {
            case GameState.PRE_FLOP:
                return _preflopStrongThreshold;
            case GameState.FLOP:
                return _flopStrongThreshold;
            case GameState.TURN:
                return _turnStrongThreshold;
            case GameState.RIVER:
            case GameState.SHOWDOWN:
            case GameState.ROUND_END:
                return _riverStrongThreshold;
            default:
                return _riverStrongThreshold;
        }
    }

    private (float preEventSeconds, float postEventSeconds) GetCaptureWindowSeconds(ObservationType observationType)
    {
        switch (observationType)
        {
            case ObservationType.HoleCards:
            case ObservationType.Flop:
            case ObservationType.Turn:
            case ObservationType.River:
                return (_dealPreEventSeconds, _dealPostEventSeconds);
            default:
                return (_actionPreEventSeconds, _actionPostEventSeconds);
        }
    }

    private bool IsPremiumPairAtEarlyStage(GameState stage, HandStrengthSnapshot strength)
    {
        if (stage != GameState.PRE_FLOP && stage != GameState.FLOP)
        {
            return false;
        }

        return strength.Ranking == HandRanking.PAIR &&
               strength.PairRank >= _premiumPairRankThreshold;
    }

    private int GetPairRankFromEncodedValue(uint encodedValue)
    {
        return (int)((encodedValue >> 16) & 0xF);
    }

    private void ResetPatternManager()
    {
        _patternManager = new PatternManager(_patternMatchThreshold, _enableAutoThresholdCalibration, _autoThresholdStdMultiplier, _minAutoCalibrationSamples);
    }

    private string GetPatternFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "face_pattern_memory.json");
    }

    private bool IsPatternDataCompatible(PatternManagerDto dto)
    {
        if (dto == null || dto.version != PatternManagerDto.CurrentVersion)
        {
            return false;
        }

        if (dto.featureVectorLength <= 0)
        {
            return false;
        }

        int expectedLength = ResolveExpectedFeatureVectorLength();
        if (expectedLength > 0 && expectedLength != dto.featureVectorLength)
        {
            return false;
        }

        return true;
    }

    private int ResolveExpectedFeatureVectorLength()
    {
        if (_faceTracker != null)
        {
            int landmarksLength = _faceTracker.CurrentLandmarksLength;
            if (landmarksLength > 0)
            {
                // For the mean, variance and max abs value + motion energy
                return landmarksLength * 3 + 1;
            }
        }

        return _patternManager != null ? _patternManager.FeatureVectorLength : 0;
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
        {
            _gameManager.OnHoleCardsDealt -= HandleHoleCardsDealt;
            _gameManager.OnFlopDealt -= HandleFlopDealt;
            _gameManager.OnTurnDealt -= HandleTurnDealt;
            _gameManager.OnRiverDealt -= HandleRiverDealt;
        }

        if (_buttonManager != null)
        {
            _buttonManager.OnPlayerFolded -= HandlePlayerFolded;
            _buttonManager.OnPlayerChecked -= HandlePlayerChecked;
            _buttonManager.OnPlayerCalled -= HandlePlayerCalled;
            _buttonManager.OnPlayerRaised -= HandlePlayerRaised;
            _buttonManager.OnLoadExistingPatternsSelected -= HandleLoadExistingPatternsSelected;
        }

        if (_turnManager != null)
        {
            _turnManager.OnWinnerDetermined -= HandleShowdownResolved;
            _turnManager.OnRoundEnded -= HandleRoundEnded;
        }
    }
}
