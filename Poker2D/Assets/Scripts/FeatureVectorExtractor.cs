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
        List<float[]> deltas = ComputeDeltas(aligned);

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

    List<float[]> ComputeDeltas(List<float[]> aligned)
    {
        List<float[]> result = new List<float[]>();

        for (int i = 1; i < aligned.Count; i++)
        {
            float[] deltas = new float[aligned[i].Length];

            for (int j = 0; j < deltas.Length; j++)
                deltas[j] = aligned[i][j] - aligned[i - 1][j];

            result.Add(deltas);
        }

        return result;
    }

    float[] Aggregate(List<float[]> deltas)
    {
        int dim = deltas[0].Length;
        float[] mean = new float[dim];

        foreach (var delta in deltas)
            for (int i = 0; i < dim; i++)
                mean[i] += delta[i];

        for (int i = 0; i < dim; i++)
            mean[i] /= deltas.Count;

        return mean;
    }
}
