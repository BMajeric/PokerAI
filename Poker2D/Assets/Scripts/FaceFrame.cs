using UnityEngine;

public class FaceFrame
{
    public float timestamp;
    // All landmarks for that one frame flattened -> [x1,y1,z1,x2,y2,z2,...]
    public float[] landmarks;

    public FaceFrame(float time, float[] landmarkData)
    {
        timestamp = time;
        landmarks = landmarkData;
    }
}
