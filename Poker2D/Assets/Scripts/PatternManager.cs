using System.Collections.Generic;
using UnityEngine;

public class PatternManager
{
    public List<Pattern> patterns = new List<Pattern>();
    public float threshold = 0.5f;

    public Pattern FindOrCreate(float[] featureVector)
    {
        Pattern best = null;
        float bestDist = float.MaxValue;

        foreach (var pattern in patterns)
        {
            float distance = Distance(featureVector, pattern.centroid);
            if (distance < bestDist)
            {
                bestDist = distance;
                best = pattern;
            }
        }

        if (best != null && bestDist < threshold)
            return best;

        Pattern newPattern = new Pattern(featureVector);
        patterns.Add(newPattern);
        return newPattern;
    }

    float Distance(float[] a, float[] b)
    {
        // If the dimensions don't match there has been a mistake so return the maximum possible distance
        if (a.Length != b.Length)
            return float.MaxValue;

        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return Mathf.Sqrt(sum);
    }
}
