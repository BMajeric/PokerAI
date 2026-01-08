using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FacialFeatureTesting : MonoBehaviour
{
    public const int MAX_FACES = 4;

    [SerializeField]
    private GameObject featurePointPrefab;
    [SerializeField]
    private GameObject referenceFeaturePointPrefab;
    private GameObject[] fpList;
    Dictionary<KeyValuePair<int, int>, float[]> fpDict;

    int FP_START_GROUP_INDEX;
    int FP_END_GROUP_INDEX;
    int length;
    int[] groupSizes;

    FDP[] fdpArray = new FDP[MAX_FACES];
    private float[] rawfdp = new float[2000];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FP_START_GROUP_INDEX = VisageTrackerNative._getFP_START_GROUP_INDEX();
        FP_END_GROUP_INDEX = VisageTrackerNative._getFP_END_GROUP_INDEX();
        length = FP_END_GROUP_INDEX - FP_START_GROUP_INDEX + 1;
        groupSizes = new int[length];
        VisageTrackerNative._getGroupSizes(groupSizes, length);

        fpList = new GameObject[groupSizes.Sum()];

        // Fill the fdpArray with FDP elements to avoid Null reference exceptions when filling it
        for (int i = 0; i < MAX_FACES; i++)
        {
            fdpArray[i] = new FDP();
        }

        FillfpDict();

        // Create feature point game objects
        int counter = 0;
        foreach (var entry in fpDict)
        {
            int firstIndex = entry.Key.Key;    // Group number
            int secondIndex = entry.Key.Value;  // Index 
            float[] coordinates = entry.Value;  // [x, y, z] coordinates

            float x = coordinates[0];
            float y = coordinates[1];
            float z = coordinates[2];

            Debug.Log($"Feature point ({firstIndex}, {secondIndex}): x={x}, y={y}, z={z}");

            // Color the reference point differently
            GameObject featurePoint = Instantiate((firstIndex == 12 && secondIndex == 1) ? referenceFeaturePointPrefab : featurePointPrefab, Vector3.zero, Quaternion.identity);
            fpList[counter] = featurePoint;
            counter++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFacialFeatureCoordinates();
    }

    public void FillfpDict()
    {
        for (int faceIndex = 0; faceIndex < MAX_FACES; faceIndex++)
        {
            VisageTrackerNative._getAllFeaturePoints3D(rawfdp, rawfdp.Length, faceIndex);
            fdpArray[faceIndex].Fill(rawfdp);
        }
        Debug.Log($"FDP: {fdpArray}");

        fpDict = fdpArray[0].getFeaturePointDict();
    }

    private void UpdateFacialFeatureCoordinates()
    {
        FillfpDict();

        int counter = 0;
        foreach (var entry in fpDict)
        {
            int firstIndex = entry.Key.Key;    // First part of the pair (2 in your example)
            int secondIndex = entry.Key.Value;  // Second part of the pair (1 in your example)
            float[] coordinates = entry.Value;  // [x, y, z]

            // Access individual coordinates
            float x = coordinates[0];
            float y = coordinates[1];
            float z = coordinates[2];

            Debug.Log($"Feature point ({firstIndex}, {secondIndex}): x={x}, y={y}, z={z}");

            float[] rawPos = fdpArray[0].getFPPos(firstIndex, secondIndex);
            Vector3 pos = new Vector3(rawPos[0] * 40, rawPos[1] * 40, rawPos[2] * 40);
            fpList[counter].transform.position = pos;

            counter++;
        }
    }
}
