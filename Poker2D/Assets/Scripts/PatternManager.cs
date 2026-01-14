using System.Collections.Generic;
using UnityEngine;

public class PatternManager
{
    public List<Pattern> patterns = new List<Pattern>();
    public float threshold = 0.5f;

    public Pattern FindOrCreate(float[] featureVector, out bool isNew)
    {
        Pattern best = null;
        float bestDist = float.MaxValue;

        // Find the pattern that matches our pattern the most
        foreach (var pattern in patterns)
        {
            float distance = Distance(featureVector, pattern.centroid);
            if (distance < bestDist)
            {
                bestDist = distance;
                best = pattern;
            }
        }

        // If the pattern is close enough, match it
        if (best != null && bestDist < threshold)
        {
            isNew = false;
            return best;
        }

        // If pattern is not close enough, create a new one
        Pattern newPattern = new Pattern(featureVector);
        patterns.Add(newPattern);
        isNew = true;
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
