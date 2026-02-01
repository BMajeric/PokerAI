public class Pattern
{
    public int id;
    public float[] centroid;
    public int count;

    public int successfulBluffCount;
    public int strongAggressiveCount;
    public int strongPassiveCount;
    public int weakAggressiveCount;     // This represents a bluff atempt
    public int weakPassiveCount;

    public Pattern(float[] initial, int id)
    {
        centroid = initial;
        this.id = id;
        count = 0;
    }

    // The constructor for restoring patterns by loading them from memory
    public Pattern(float[] initial, int id, int count, int successfulBluffCount, int strongAggressiveCount, int strongPassiveCount, int weakAggressiveCount, int weakPassiveCount)
    {
        centroid = initial;
        this.id = id;
        this.count = count;
        this.successfulBluffCount = successfulBluffCount;
        this.strongAggressiveCount = strongAggressiveCount;
        this.strongPassiveCount = strongPassiveCount;
        this.weakAggressiveCount = weakAggressiveCount;
        this.weakPassiveCount = weakPassiveCount;
    }

    // Create Data transfer object from pattern
    public PatternDto ToDto()
    {
        return new PatternDto
        {
            id = id,
            centroid = centroid,
            count = count,
            successfulBluffCount = successfulBluffCount,
            strongAggressiveCount = strongAggressiveCount,
            strongPassiveCount = strongPassiveCount,
            weakAggressiveCount = weakAggressiveCount,
            weakPassiveCount = weakPassiveCount
        };
    }

    public void Update(float[] sample, bool isStrongHand, bool isAggressive, bool didWin)
    {
        count++;

        for (int i = 0; i < centroid.Length; i++)
            centroid[i] += (sample[i] - centroid[i]) / count;

        if (isStrongHand)
        {
            if (isAggressive)
                strongAggressiveCount++;
            else
                strongPassiveCount++;
        }
        else
        {
            if (isAggressive)
            {
                weakAggressiveCount++;
                if (didWin)
                    successfulBluffCount++;
            }
            else
            {
                weakPassiveCount++;
            }
        }
    }
}
