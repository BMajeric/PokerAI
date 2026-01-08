using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

public enum FDPmember
{
    x = 0,
    y = 1,
    z = 2, 
    defined = 3,
    detected = 4,
    quality = 5
}

public struct VsRect
{
    public int x;
    public int y;
    public int width;
    public int height;
}

public class FDP
{
    private Dictionary<KeyValuePair<int, int>, float[]> featurePoints = new Dictionary<KeyValuePair<int, int>, float[]>();

    public void Fill(float[] rawFDP)
    {
        int FP_START_GROUP_INDEX = VisageTrackerNative._getFP_START_GROUP_INDEX();
        int FP_END_GROUP_INDEX = VisageTrackerNative._getFP_END_GROUP_INDEX();
        int length = FP_END_GROUP_INDEX - FP_START_GROUP_INDEX + 1;
        int[] groupSizes = new int[length];
        VisageTrackerNative._getGroupSizes(groupSizes, length);

        int bufferIndex = 0;
        for (int group = FP_START_GROUP_INDEX; group <= FP_END_GROUP_INDEX; group++)
        {
            for (int index = 1; index <= groupSizes[group - FP_START_GROUP_INDEX]; index++)
            {
                KeyValuePair<int, int> groupIndex = new KeyValuePair<int, int>(group, index);

                float[] featurePoint = new float[6]
                {
                    rawFDP[bufferIndex    ],
                    rawFDP[bufferIndex + 1],
                    rawFDP[bufferIndex + 2],
                    (int)rawFDP[bufferIndex + 3],
                    (int)rawFDP[bufferIndex + 4],
                    rawFDP[bufferIndex + 5]
                };
           
                bufferIndex += 6;

                if (!featurePoints.ContainsKey(groupIndex))
                {
                    featurePoints.Add(groupIndex, featurePoint);
                }
                else
                {
                    featurePoints[groupIndex] = featurePoint;
                }
            }
        }
    }

    public Dictionary<KeyValuePair<int, int>, float[]> getFeaturePointDict()
    {
        return featurePoints;
    }

    public float[] getFPPos(int group, int index)
    {
        float[] position = new float[3];
        KeyValuePair<int, int> groupIndex = new KeyValuePair<int, int>(group, index);
        for (int i = 0; i < 3; ++i)
        {
            position[i] = featurePoints[groupIndex][i];
        }

        return position;
    }

    public int FPIsDefined(int group, int index)
    {
        int isDef;
        KeyValuePair<int, int> groupIndex = new KeyValuePair<int, int>(group, index);
        isDef = (int)featurePoints[groupIndex][(int)FDPmember.defined];
        return isDef;
    }

    public int FPIsDetected(int group, int index)
    {
        int isDet;
        KeyValuePair<int, int> groupIndex = new KeyValuePair<int, int>(group, index);
        isDet = (int)featurePoints[groupIndex][(int)FDPmember.detected];
        return isDet;
    }

    // quality information returned exclusively with _getAllFeaturePoints2D and _getFeaturePoints2D functions. 
    public float getFPQuality(int group, int index)
    {
        float qual;
        KeyValuePair<int, int> groupIndex = new KeyValuePair<int, int>(group, index);
        qual = featurePoints[groupIndex][(int)FDPmember.quality];
        return qual;
    } 
}

