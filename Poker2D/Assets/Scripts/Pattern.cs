public class Pattern
{
    public float[] centroid;
    public int count;

    public int bluffCount;
    public int strongHandCount;

    public Pattern(float[] initial)
    {
        centroid = initial;
        count = 1;
    }

    public void Update(float[] sample, bool wasBluff)
    {
        count++;

        for (int i = 0; i < centroid.Length; i++)
            centroid[i] += (sample[i] - centroid[i]) / count;

        if (wasBluff) bluffCount++;
        else strongHandCount++;
    }
}
