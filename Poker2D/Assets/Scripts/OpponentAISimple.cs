using UnityEngine;

public class OpponentAISimple : Player
{
    private const float BaseRaiseChance = 0.15f;
    private const float BaseFoldChance = 0.15f;
    private const float MinTendencyConfidence = 0.4f;
    private const float MaxBiasShift = 0.5f;

    private FacePatternLearningCoordinator _facePatternLearningCoordinator;

    public void SetFacePatternLearningCoordinator(FacePatternLearningCoordinator coordinator)
    {
        _facePatternLearningCoordinator = coordinator;
    }

    public (PlayerAction action, int amount) MakeDecision(GameStateSnapshot state)
    {
        // Fall back to baseline if no useful tendency is calculated
        float raiseChance = BaseRaiseChance;
        float foldChance = BaseFoldChance;
        PlayerTendency tendency = PlayerTendency.None;
        bool useTendency = false;

        // Find the tendency and determine its usefulness
        if (_facePatternLearningCoordinator != null)
        {
            FacePatternLearningCoordinator.ObservationType actionType = ResolveObservationType(state);
            tendency = _facePatternLearningCoordinator.GetPlayerTendency(state.GameState, actionType);
            useTendency = tendency.HasData && tendency.Confidence >= MinTendencyConfidence;
        }

        // If tendency is useful, update raise and fold probabilities accordingly
        if (useTendency)
        {
            // Calculate how agressive the AI should be with betting
            //          - bluff probability high -> aggression bias > 0 -> AI should raise more often
            //          - strong hand probability high -> aggression bias < 0 -> AI should fold more often
            float aggressionBias = Mathf.Clamp(tendency.BluffProbability - tendency.StrongProbability, -1f, 1f);

            // Bias shift makes the maximum value of raise/fold chance increase = MaxBiasShift
            float biasShift = MaxBiasShift * tendency.Confidence;

            // Clamp the raise and fold chances to prevent negative chances
            raiseChance = Mathf.Clamp01(raiseChance + aggressionBias * biasShift);
            foldChance = Mathf.Clamp01(foldChance - aggressionBias * biasShift);
        }

        // Set default AI calls if it decides not to raise/fold
        (PlayerAction action, int amount) response;
        if (state.PlayerPot > state.OpponentPot)
        {
            response = (PlayerAction.CALL, state.PlayerPot - state.OpponentPot);
        }
        else
        {
            response = (PlayerAction.CHECK, 0);
        }

        // Apply the raise and fold chances to calculate the final AI decision
        float chance = Random.value;
        if (chance <= raiseChance)
        {
            int amount = Random.Range(state.PlayerPot - state.OpponentPot + 1, Mathf.Min((state.PlayerChips + (state.PlayerPot - state.OpponentPot)), state.OpponentChips) + 1);
            response = (PlayerAction.RAISE, amount);
        }
        else if (chance >= 1f - foldChance)
        {
            response = (PlayerAction.FOLD, 0);
        }

        return response;
    }

    private FacePatternLearningCoordinator.ObservationType ResolveObservationType(GameStateSnapshot state)
    {
        if (state.PlayerPot > state.OpponentPot)
        {
            return FacePatternLearningCoordinator.ObservationType.PlayerRaise;
        }

        return FacePatternLearningCoordinator.ObservationType.PlayerCheck;
    }
}
