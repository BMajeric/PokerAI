using System.Collections.Generic;
using UnityEngine;

public class PatternManager
{
    public List<Pattern> patterns = new List<Pattern>();
    public float threshold;
    public bool autoCalibrateThreshold;
    public float calibrationStdMultiplier;
    public int minCalibrationSamples;

    private readonly RunningStats _matchedDistanceStats = new RunningStats();
    private readonly RunningStats _newDistanceStats = new RunningStats();

    // Getter for feature vector length
    public int FeatureVectorLength
    {
        get
        {
            foreach (Pattern pattern in patterns)
            {
                if (pattern != null && pattern.centroid != null)
                {
                    return pattern.centroid.Length;
                }
            }

            return 0;
        }
    }

    public PatternManager(float threshold = 55f, 
                          bool autoCalibrateThreshold = false, 
                          float calibrationStdMultiplier = 2f, 
                          int minCalibrationSamples = 10)
    {
        this.threshold = threshold;
        this.autoCalibrateThreshold = autoCalibrateThreshold;
        this.calibrationStdMultiplier = calibrationStdMultiplier;
        this.minCalibrationSamples = minCalibrationSamples;

    }

    public Pattern FindOrCreate(float[] featureVector, out bool isNew, out float bestDistance)
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
            bestDistance = bestDist;
            RecordDistanceStats(bestDist, isNew);
            return best;
        }

        // If pattern is not close enough or not found, create a new one
        Pattern newPattern = new Pattern(featureVector);
        patterns.Add(newPattern);
        isNew = true;
        bestDistance = bestDist;
        if (best != null)
            RecordDistanceStats(bestDist, isNew);
        return newPattern;
    }

    public bool TryGetClosestPattern(float[] featureVector, out Pattern closestPattern, out float bestDistance)
    {
        // Try to find the closest pattern without adding it into the pattern list or creating a new pattern
        closestPattern = null;
        bestDistance = float.MaxValue;

        foreach (var pattern in patterns)
        {
            float distance = Distance(featureVector, pattern.centroid);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closestPattern = pattern;
            }
        }

        if (closestPattern == null || bestDistance >= threshold)
        {
            closestPattern = null;
            return false;
        }

        return true;
    }

    float Distance(float[] a, float[] b)
    {
        // If the dimensions don't match there has been a mistake so return the maximum possible distance
        if (a.Length != b.Length)
            return float.MaxValue;

        // Calculate Euclidean distance
        float sum = 0f;
        for (int i = 0; i < a.Length; i++)
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        return Mathf.Sqrt(sum);
    }

    private void RecordDistanceStats(float distance, bool isNew)
    {
        if (isNew)
        {
            _newDistanceStats.AddSample(distance);
            LogDistanceStats("new-pattern", _newDistanceStats);
        }
        else
        {
            _matchedDistanceStats.AddSample(distance);
            LogDistanceStats("same-pattern", _matchedDistanceStats);
            TryAutoCalibrateThreshold();
        }
    }

    private void LogDistanceStats(string label, RunningStats stats)
    {
        Debug.Log($"PatternManager: {label} distances mean={stats.Mean:F4} std={stats.StandardDeviation:F4} n={stats.Count}.");
    }

    private void TryAutoCalibrateThreshold()
    {
        if (!autoCalibrateThreshold || _matchedDistanceStats.Count < minCalibrationSamples)
        {
            return;
        }

        float suggested = _matchedDistanceStats.Mean + calibrationStdMultiplier * _matchedDistanceStats.StandardDeviation;
        if (Mathf.Approximately(suggested, threshold))
        {
            return;
        }

        threshold = suggested;
        Debug.Log($"PatternManager: auto-calibrated threshold={threshold:F4} (mean={_matchedDistanceStats.Mean:F4}, std={_matchedDistanceStats.StandardDeviation:F4}, k={calibrationStdMultiplier:F2}).");
    }

    // Create Data transfer object from the pattern manager containing all patterns
    public PatternManagerDto ToDto(int featureVectorLength)
    {
        PatternManagerDto dto = new PatternManagerDto
        {
            featureVectorLength = featureVectorLength
        };

        foreach (Pattern pattern in patterns)
        {
            if (pattern == null)
            {
                continue;
            }

            dto.patterns.Add(pattern.ToDto());
        }

        return dto;
    }

    // Read saved DTO and load all the data into the pattern manager
    public void LoadFromDto(PatternManagerDto dto)
    {
        patterns.Clear();

        // If no patterns exist in the DTO, start with a clean pattern manager
        if (dto == null || dto.patterns == null)
        {
            return;
        }

        foreach (PatternDto patternDto in dto.patterns)
        {
            if (patternDto == null || patternDto.centroid == null)
            {
                continue;
            }

            Pattern pattern = new Pattern(
                patternDto.centroid,
                patternDto.count,
                patternDto.successfulBluffCount,
                patternDto.strongAggressiveCount,
                patternDto.strongPassiveCount,
                patternDto.weakAggressiveCount,
                patternDto.weakPassiveCount);
            patterns.Add(pattern);
        }
    }

    private class RunningStats
    {
        public int Count { get; private set; }
        public float Mean { get; private set; }
        public float StandardDeviation => Count > 1 ? Mathf.Sqrt(_m2 / (Count - 1)) : 0f;

        private float _m2;

        public void AddSample(float sample)
        {
            // Update stats using Welford's method for computing variance
            Count++;
            float delta = sample - Mean;
            Mean += delta / Count;
            float delta2 = sample - Mean;
            _m2 += delta * delta2;
        }
    }
}
