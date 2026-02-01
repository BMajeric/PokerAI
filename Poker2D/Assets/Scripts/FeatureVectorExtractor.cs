using System.Collections.Generic;
using UnityEngine;

public class FeatureVectorExtractor
{
    int referenceIndex;
    float[] runningMean;
    float[] runningVarianceAccumulator;
    int runningCount;

    public FeatureVectorExtractor(int referenceFeaturePointIndex)
    {
        referenceIndex = referenceFeaturePointIndex;
    }

    public float[] Extract(List<FaceFrame> frames)
    {
        if (frames.Count < 2)
            return null;

        // Align frames to reference point and normalize scale
        // so deltas are the same whether the player is closer or farther away from the screen
        List<float[]> aligned = Align(frames);

        // Temporal deltas
        List<float[]> deltas = ComputeDeltas(frames, aligned);

        // Aggregate
        return Aggregate(deltas);
    }

    List<float[]> Align(List<FaceFrame> frames)
    {
        List<float[]> result = new List<float[]>();

        foreach (var frame in frames)
        {
            float[] aligned = new float[frame.landmarks.Length];
            int refOffset = referenceIndex * 3;

            float xRef = frame.landmarks[refOffset];
            float yRef = frame.landmarks[refOffset + 1];
            float zRef = frame.landmarks[refOffset + 2];

            for (int i = 0; i < frame.landmarks.Length; i += 3)
            {
                aligned[i] = frame.landmarks[i] - xRef;
                aligned[i + 1] = frame.landmarks[i + 1] - yRef;
                aligned[i + 2] = frame.landmarks[i + 2] - zRef;
            }

            // Normalize scale using average distance from the reference landmark to all landmarks
            // to keep deltas stable regardless of face distance to the camera
            // Effectively scales features to set mean distance from the reference point to around 1
            float scale = 0f;
            int pointCount = aligned.Length / 3;
            for (int i = 0; i < aligned.Length; i += 3)
            {
                float dx = aligned[i];
                float dy = aligned[i + 1];
                float dz = aligned[i + 2];
                scale += dx * dx + dy * dy + dz * dz;
            }

            float scaleFactor = pointCount > 0 ? Mathf.Sqrt(scale / pointCount) : 1f;   // To avoid division by 0
            if (scaleFactor > 0.0001f)
            {
                for (int i = 0; i < aligned.Length; i++)
                    aligned[i] /= scaleFactor;
            }

            result.Add(aligned);
        }

        return result;
    }

    List<float[]> ComputeDeltas(List<FaceFrame> frames, List<float[]> aligned)
    {
        List<float[]> result = new List<float[]>();

        for (int i = 1; i < aligned.Count; i++)
        {
            float[] deltas = new float[aligned[i].Length];

            // Normalize the deltas using the time difference between frames
            // to make them more stable across different frame rates
            float timeDelta = frames[i].timestamp - frames[i - 1].timestamp;
            float normalizer = timeDelta > 0f ? timeDelta : 1f;     // To avoid division by 0

            for (int j = 0; j < deltas.Length; j++)
                deltas[j] = (aligned[i][j] - aligned[i - 1][j]) / normalizer;

            result.Add(deltas);
        }

        return result;
    }

    float[] Aggregate(List<float[]> deltas)
    {
        int dim = deltas[0].Length;
        float[] mean = new float[dim];
        float[] variance = new float[dim];
        float[] maxAbsDistance = new float[dim];
        float motionEnergy = 0f;

        // delta -> list of deltas of each point between two consecutive frames
        foreach (var delta in deltas)
            for (int i = 0; i < dim; i++)
            {
                float value = delta[i];
                mean[i] += value;
                float absValue = Mathf.Abs(value);
                if (absValue > maxAbsDistance[i])
                    maxAbsDistance[i] = absValue;
                motionEnergy += value * value;
            }

        for (int i = 0; i < dim; i++)
            mean[i] /= deltas.Count;

        foreach (var delta in deltas)
            for (int i = 0; i < dim; i++)
            {
                float diff = delta[i] - mean[i];
                variance[i] += diff * diff;
            }

        for (int i = 0; i < dim; i++)
            variance[i] /= deltas.Count;

        // Aggregate features into single feature vector
        // Feature order: mean (dim), variance (dim), max abs (dim), motion energy (1)
        float[] aggregated = new float[dim * 3 + 1];
        int offset = 0;

        System.Array.Copy(mean, 0, aggregated, offset, dim);
        offset += dim;

        System.Array.Copy(variance, 0, aggregated, offset, dim);
        offset += dim;

        System.Array.Copy(maxAbsDistance, 0, aggregated, offset, dim);
        offset += dim;

        aggregated[offset] = motionEnergy;

        // Standardize final feature vector per feature using the running mean and std to keep scales comparable
        // so the algorithm doesn't bias one statistic over all the others because it has a larger scale
        StandardizeFeatureVector(aggregated);

        return aggregated;
    }

    void StandardizeFeatureVector(float[] featureVector)
    {
        // Initialize values 
        if (runningMean == null || runningMean.Length != featureVector.Length)
        {
            runningMean = new float[featureVector.Length];
            runningVarianceAccumulator = new float[featureVector.Length];
            runningCount = 0;
        }

        // Calculate running mean and variance using Welford's method for computing variance
        runningCount++;
        float count = runningCount;
        for (int i = 0; i < featureVector.Length; i++)
        {
            float value = featureVector[i];
            float delta = value - runningMean[i];
            runningMean[i] += delta / count;
            float delta2 = value - runningMean[i];
            runningVarianceAccumulator[i] += delta * delta2;

            // Represent each feature with its z-score calculated from a running mean and variance
            if (runningCount > 1)
            {
                float variance = runningVarianceAccumulator[i] / (runningCount - 1);
                float stdDev = Mathf.Sqrt(variance);
                featureVector[i] = stdDev > 0.0001f ? (value - runningMean[i]) / stdDev : 0f;
            }
            else
            {
                featureVector[i] = 0f;
            }
        }
    }

}
