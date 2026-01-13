using System.Collections.Generic;
using UnityEngine;

public class FeatureVectorExtractor
{
    int referenceIndex;

    public FeatureVectorExtractor(int referenceFeaturePointIndex)
    {
        referenceIndex = referenceFeaturePointIndex;
    }

    public float[] Extract(List<FaceFrame> frames)
    {
        if (frames.Count < 2)
            return null;

        // Align frames to reference point
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
            float normalizer = timeDelta > 0f ? timeDelta : 1f;     // To avoid devision by 0

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

        return aggregated;
    }
}
