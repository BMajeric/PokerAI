using System.Collections.Generic;
using UnityEngine;

public class PreflopEventExtractor
{
    private float preOffset = 0.0f;
    private float postOffset = 1.5f;

    public List<FaceFrame> CaptureEvent(FaceFrameBuffer buffer, float eventTime)
    {
        float start = eventTime - preOffset;
        float end = eventTime + postOffset;

        return buffer.GetFramesInRange(start, end);
    }
}
