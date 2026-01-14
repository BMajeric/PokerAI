using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacePatternLearningCoordinator : MonoBehaviour
{
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

    [Header("Bluff Detection")]
    [SerializeField] private HandRanking _preflopStrongThreshold = HandRanking.PAIR;
    [SerializeField] private HandRanking _flopStrongThreshold = HandRanking.PAIR;
    [SerializeField] private HandRanking _turnStrongThreshold = HandRanking.TWO_PAIR;
    [SerializeField] private HandRanking _riverStrongThreshold = HandRanking.TWO_PAIR;
    [SerializeField] private int _premiumPairRankThreshold = 11;    // Premium pair = a pair of Jacks, Queens, Kings or Aces

    private FaceFrameBuffer _faceFrameBuffer;
    private FeatureVectorExtractor _featureVectorExtractor;
    private PatternManager _patternManager;

    private readonly List<Observation> _pendingObservations = new List<Observation>();

    private enum ObservationType
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
        _patternManager = new PatternManager();

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
    }

    private void HandleRoundEnded(Player winner)
    {
        _pendingObservations.Clear();
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
        _pendingObservations.Add(new Observation
        {
            Type = observationType,
            Timestamp = eventTime,
            FeatureVector = featureVector,
            Stage = stage
        });
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
            Pattern pattern = _patternManager.FindOrCreate(observation.FeatureVector, out bool isNew);
            if (!isNew)
                pattern.Update(observation.FeatureVector, isStrongHand, isAggressive, didPlayerWin);
        }

        _pendingObservations.Clear();
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
        }

        if (_turnManager != null)
        {
            _turnManager.OnWinnerDetermined -= HandleShowdownResolved;
            _turnManager.OnRoundEnded -= HandleRoundEnded;
        }
    }
}
