using System.Collections.Generic;
using UnityEngine;

public class FaceFrameBuffer
{
    private Queue<FaceFrame> buffer = new Queue<FaceFrame>();
    private float maxDurationSeconds;

    public FaceFrameBuffer(float maxDuration)
    {
        maxDurationSeconds = maxDuration;
    }

    public void AddFrame(FaceFrame frame)
    {
        // Add new frame
        buffer.Enqueue(frame);

        // Remove old frames if the buffer frame count exceeds the max duration
        while (buffer.Count > 0 && frame.timestamp - buffer.Peek().timestamp > maxDurationSeconds)
        {
            buffer.Dequeue();
        }
    }

    public List<FaceFrame> GetFramesInRange(float startTime, float endTime)
    {
        List<FaceFrame> result = new List<FaceFrame>();

        foreach (var frame in buffer)
        {
            if (frame.timestamp >= startTime && frame.timestamp <= endTime)
            {
                result.Add(frame);
            }
        }

        return result;
    }
}