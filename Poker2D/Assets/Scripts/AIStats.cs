using System.IO;
using System.Collections.Generic;
using System.Text;

public class AIStats
{
    // Private class to save statistics that will be exported to CVS for AI analysis
    private class DecisionRecord
    {
        public int Round { get; }
        public bool UsedTendency { get; }
        public float Confidence { get; }
        public float BluffProbability { get; }
        public float StrongProbability { get; }
        public float WeakProbability { get; }
        public string InferenceLabel { get; }
        public PlayerAction Action { get; }
        public bool Aggressive { get; }
        public bool ReactionAligned { get; }
        public bool? PlayerStrong { get; set; }
        public bool? InferenceCorrect { get; set; }

        public DecisionRecord(
            int round,
            bool usedTendency,
            float confidence,
            float bluffProbability,
            float strongProbability,
            float weakProbability,
            string inferenceLabel,
            PlayerAction action,
            bool aggressive,
            bool reactionAligned)
        {
            Round = round;
            UsedTendency = usedTendency;
            Confidence = confidence;
            BluffProbability = bluffProbability;
            StrongProbability = strongProbability;
            WeakProbability = weakProbability;
            InferenceLabel = inferenceLabel;
            Action = action;
            Aggressive = aggressive;
            ReactionAligned = reactionAligned;
        }
    }

    public int Rounds { get; private set; }
    public int Folds { get; private set; }
    public int Calls { get; private set; }
    public int Raises { get; private set; }
    public int Checks { get; private set; }
    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public int AggressiveActions { get; private set; }
    public int PassiveActions { get; private set; }
    public int InferredWeakResponses { get; private set; }
    public int InferredBluffResponses { get; private set; }
    public int InferredStrongResponses { get; private set; }
    public int InferenceCorrectCount { get; private set; }
    public int InferenceIncorrectCount { get; private set; }

    private readonly List<DecisionRecord> _decisionRecords = new List<DecisionRecord>();
    private int _exportedDecisionCount;
    private int _currentRound;

    public void RecordRoundStarted()
    {
        Rounds++;
        _currentRound = Rounds;
    }

    public void RecordAction(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.FOLD:
                Folds++;
                break;
            case PlayerAction.CALL:
                Calls++;
                break;
            case PlayerAction.RAISE:
                Raises++;
                break;
            case PlayerAction.CHECK:
                Checks++;
                break;
        }

        if (action == PlayerAction.RAISE || action == PlayerAction.CALL)
        {
            AggressiveActions++;
        }
        else
        {
            PassiveActions++;
        }
    }

    public void RecordDecision(PlayerAction action, bool usedTendency, PlayerTendency tendency)
    {
        // Fist ensure there are safe fallback options if tendency is not used
        string inferenceLabel = "none";
        float confidence = 0f;
        float bluffProbability = 0f;
        float strongProbability = 0f;
        float weakProbability = 0f;

        if (usedTendency)
        {
            confidence = tendency.Confidence;
            bluffProbability = tendency.BluffProbability;
            strongProbability = tendency.StrongProbability;
            weakProbability = tendency.WeakProbability;
            inferenceLabel = weakProbability >= strongProbability ? "weak" : "strong";
        }

        // Calculate if the AI's action was aggressive or not
        bool aggressive = action == PlayerAction.RAISE || action == PlayerAction.CALL;
        bool reactionAligned = false;

        // If tendency was used see if the hand was predicted to be weak or strong
        //      - if it was weak -> AI action should have been aggressive
        //      - if it was weak -> AI action should have been passive
        if (usedTendency)
        {
            reactionAligned = inferenceLabel == "weak" ? aggressive : !aggressive;
        }

        _decisionRecords.Add(new DecisionRecord(
            _currentRound,
            usedTendency,
            confidence,
            bluffProbability,
            strongProbability,
            weakProbability,
            inferenceLabel,
            action,
            aggressive,
            reactionAligned));
    }

    public void RecordShowdownResult(bool playerStrong)
    {
        // Go through all the decision records backwards to look at the relevant ones first
        for (int i = _decisionRecords.Count - 1; i >= 0; i--)
        {
            DecisionRecord record = _decisionRecords[i];

            // If we get out of the current round scope break so that we don't look at past rounds
            if (record.Round != _currentRound)
            {
                break;
            }

            // If the record has players strength ground truth filled we skip it 
            if (record.PlayerStrong.HasValue)
            {
                continue;
            }


            record.PlayerStrong = playerStrong;

            // If nothing was inferred skip it
            if (!record.UsedTendency || record.InferenceLabel == "none")
            {
                continue;
            }

            // Check if the inferred value was the same as the ground truth
            bool inferredStrong = record.InferenceLabel == "strong";
            bool isCorrect = inferredStrong == playerStrong;
            record.InferenceCorrect = isCorrect;

            if (isCorrect)
            {
                InferenceCorrectCount++;
            }
            else
            {
                InferenceIncorrectCount++;
            }
        }
    }

    public void RecordWin()
    {
        Wins++;
    }

    public void RecordLoss()
    {
        Losses++;
    }

    public void RecordInferredBluffResponse()
    {
        InferredBluffResponses++;
    }

    public void RecordInferredWeakResponse()
    {
        InferredWeakResponses++;
    }

    public void RecordInferredStrongResponse()
    {
        InferredStrongResponses++;
    }

    public string BuildSummary()
    {
        int totalActions = AggressiveActions + PassiveActions;
        float aggressiveRate = totalActions > 0 ? (float)AggressiveActions / totalActions : 0f;
        float passiveRate = totalActions > 0 ? (float)PassiveActions / totalActions : 0f;
        int totalInference = InferenceCorrectCount + InferenceIncorrectCount;
        float inferenceAccuracy = totalInference > 0 ? (float)InferenceCorrectCount / totalInference : 0f;

        return $"AI Stats | rounds={Rounds} wins={Wins} losses={Losses} folds={Folds} calls={Calls} raises={Raises} checks={Checks} " +
               $"aggressive={AggressiveActions} ({aggressiveRate:P1}) passive={PassiveActions} ({passiveRate:P1}) " +
               $"inferred weak={InferredWeakResponses} inferred bluffs={InferredBluffResponses} inferred strong={InferredStrongResponses} " +
               $"inference accuracy={inferenceAccuracy:P1}";
    }

    private string GenerateCsvHeader()
    {
        return "round,action,aggressive,reaction_aligned,used_tendency,confidence,bluff_probability,strong_probability,weak_probability,inference_label,player_strong,inference_correct," +
               "rounds_total,wins_total,losses_total,folds_total,calls_total,raises_total,checks_total,aggressive_total,passive_total,inferred_weak_total,inferred_bluff_total,inferred_strong_total," +
               "inference_correct_total,inference_incorrect_total";
    }
    
    public void CreateCsvHeader(string path)
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine(GenerateCsvHeader());

        File.WriteAllText(path, builder.ToString());
    }

    public void ExportToCsv(string path)
    {
        // In case file building is skipped somehow
        bool needsHeader = !File.Exists(path);
        StringBuilder builder = new StringBuilder();

        if (needsHeader)
        {
            builder.AppendLine(GenerateCsvHeader());
        }

        // Fill the data into CSV
        for (int i = _exportedDecisionCount; i < _decisionRecords.Count; i++)
        {
            DecisionRecord record = _decisionRecords[i];
            builder.AppendLine(
                $"{record.Round},{record.Action},{record.Aggressive},{record.ReactionAligned},{record.UsedTendency}," +
                $"{record.Confidence:F4},{record.BluffProbability:F4},{record.StrongProbability:F4},{record.WeakProbability:F4}," +
                $"{record.InferenceLabel},{record.PlayerStrong},{record.InferenceCorrect}," +
                $"{Rounds},{Wins},{Losses},{Folds},{Calls},{Raises},{Checks},{AggressiveActions},{PassiveActions}," +
                $"{InferredWeakResponses},{InferredBluffResponses},{InferredStrongResponses},{InferenceCorrectCount},{InferenceIncorrectCount}");
        }

        _exportedDecisionCount = _decisionRecords.Count;
        File.AppendAllText(path, builder.ToString());
    }
}
