public struct PlayerTendency
{
    public float BluffProbability;
    public float StrongProbability;
    public float WeakProbability;
    public float Confidence;
    public int SampleCount;

    public bool HasData => SampleCount > 0;

    public static PlayerTendency None => new PlayerTendency
    {
        BluffProbability = 0f,
        StrongProbability = 0f,
        WeakProbability = 0f,
        Confidence = 0f,
        SampleCount = 0
    };

    public static PlayerTendency FromPattern(Pattern pattern, float confidence)
    {
        // Inspect pattern to get what the player is probably doing (convert counts to probabilities)
        int total = pattern.strongAggressiveCount + pattern.strongPassiveCount + pattern.weakAggressiveCount + pattern.weakPassiveCount;
        if (total <= 0)
        {
            return new PlayerTendency
            {
                BluffProbability = 0f,
                StrongProbability = 0f,
                WeakProbability = 0f,
                Confidence = confidence,
                SampleCount = pattern.count
            };
        }

        // Calculate player intent probabilities from the counts
        float strong = (pattern.strongAggressiveCount + pattern.strongPassiveCount) / (float)total;
        float weak = (pattern.weakAggressiveCount + pattern.weakPassiveCount) / (float)total;
        float bluff = pattern.weakAggressiveCount / (float)total;

        return new PlayerTendency
        {
            BluffProbability = bluff,
            StrongProbability = strong,
            WeakProbability = weak,
            Confidence = confidence,
            SampleCount = pattern.count
        };
    }
}
