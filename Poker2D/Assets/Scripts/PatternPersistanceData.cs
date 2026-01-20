using System;
using System.Collections.Generic;

[Serializable]
public class PatternDto
{
    public int id;
    public float[] centroid;
    public int count;
    public int successfulBluffCount;
    public int strongAggressiveCount;
    public int strongPassiveCount;
    public int weakAggressiveCount;
    public int weakPassiveCount;
}

[Serializable]
public class PatternManagerDto
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int featureVectorLength;
    public List<PatternDto> patterns = new List<PatternDto>();
}
