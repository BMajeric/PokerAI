public class Pattern
{
    public float[] centroid;
    public int count;

    public int successfulBluffCount;
    public int strongAggressiveCount;
    public int strongPassiveCount;
    public int weakAggressiveCount;     // This represents a bluff atempt
    public int weakPassiveCount;

    public Pattern(float[] initial)
    {
        centroid = initial;
        count = 1;
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
