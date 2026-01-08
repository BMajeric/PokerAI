using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

[StructLayout(LayoutKind.Sequential, Size=39), Serializable]
public struct ManagedAnalysisData
{
    public float       age;
    public bool        ageValid;
    public int         gender;
    public bool        genderValid;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
    public float[]     emotionProbabilities;
    public bool        emotionsValid;
}

public enum VFAFlags {
    VFA_AGE = 1,
    VFA_GENDER = 2,
    VFA_EMOTION = 4
}

public enum VFAReturnCode {
    NoError = 0,
    InvalidFrame = 1,
    InvalidFaceData = 2,
    InvalidLicense = 3,
    DataUninitialized = 4
}

public partial class Analyser: MonoBehaviour
{
#if !UNITY_WEBGL
    #region Properties

    bool AnalyserInitialized = false;

    [Header("Face Analysis output data")]
    const int NUM_EMOTIONS = 7;

    private const double EMO_DISPLAY_TRESHOLD = 0.3;
    private ManagedAnalysisData[] analysisData = new ManagedAnalysisData[Tracker.MAX_FACES];
    private float[] age = new float[Tracker.MAX_FACES];
    private int[] gender = new int[Tracker.MAX_FACES];
    private List<float[]> EmotionListPerFace = new List<float[]>();

    [Header("Tracker object settings")]
    private int[] TrackerStatus = new int[Tracker.MAX_FACES];
    private Vector3[] RotationApparent = new Vector3[Tracker.MAX_FACES];
    private float[] rotationApparent = new float[3];

    [Header("Analysis Data Element")]
    List<GameObject> AnalysisList = new List<GameObject>();

    //Gender display variables
    private Material[] matrialList;
    private Material[] warningList;

    //Emotion display variables
    private Vector3[][] emoVertices = new Vector3[Tracker.MAX_FACES][];
    private float EmoBarSize;
    private TextMesh[][] percentage = new TextMesh[Tracker.MAX_FACES][];

    FDP[] fdpArray = new FDP[Tracker.MAX_FACES];
    private float[] rawfdp = new float[2000];

    //Background
    private BackgroundWorker fa_worker;
    private System.Object lockAnalyser = new System.Object();
    private System.Object lockWithinConstraints = new System.Object();

    #endregion

    /// <summary>
    /// Wait for background thread to finish before quitting.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (fa_worker.WorkerSupportsCancellation == true && fa_worker.IsBusy)
        {
            StartCoroutine(StopAgeGenderEmotionsEstimation());
        }
    }

    // Use this for initialization
    void Start()
    {
        string dataFilesPath = Application.streamingAssetsPath + "/" + "Visage Tracker/vfa";

        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:

                break;
			case RuntimePlatform.Android: 
			dataFilesPath = Application.persistentDataPath + "/" + "vfa";

                break;
            case RuntimePlatform.OSXPlayer:

                break;
            case RuntimePlatform.OSXEditor:

                break;
            case RuntimePlatform.WebGLPlayer:
              
                break;
            case RuntimePlatform.WindowsEditor:
                dataFilesPath = Application.streamingAssetsPath + "/Visage Tracker/vfa";
                break;
        }

        // Initialize analysis 
        AnalyserInitialized = InitializeAnalyser(dataFilesPath);

        // Intialize arrays used for face analysis
        InitializeContainers();

        // Initialize apparent rotation array
        for (int i = 0; i < Tracker.MAX_FACES; i++)
        {
            RotationApparent[i] = new Vector3(0, 0, -1000);
            EmotionListPerFace.Add(default(float[]));
        }

        // APP SPECIFIC CODE
        // warningPanel.SetActive(false);
    }

    /// <summary>
	/// Intialize arrays used for face analysis.
	/// </summary>
    private void InitializeContainers()
    {
        for (int i = 0; i < Tracker.MAX_FACES; ++i)
        {
            age[i] = -1;
            gender[i] = -1;
            EmotionListPerFace.Add(default(float[]));
            AnalysisList.Add((GameObject)Instantiate(AnalysisDataElement));
            AnalysisList[i].GetComponent<MeshFilter>().transform.position -= new Vector3(0, 0, 10000);

            fdpArray[i] = new FDP();

            emoVertices[i] = AnalysisList[i].GetComponentsInChildren<MeshFilter>()[3 + i].mesh.vertices;

            percentage[i] = new TextMesh[NUM_EMOTIONS];

            for (int j = 0; j < NUM_EMOTIONS; ++j)
            {
                percentage[i][j] = AnalysisList[i].GetComponentsInChildren<TextMesh>()[1 + j];
            }
        }

        matrialList = Resources.LoadAll("Analysis/Material", typeof(Material)).Cast<Material>().ToArray();

        warningList = Resources.LoadAll("WarningPanel/Material", typeof(Material)).Cast<Material>().ToArray();

        EmoBarSize = Math.Abs(emoVertices[0][0].x - emoVertices[0][3].x);
    }

    
    /// <summary>
    /// Initialize analyser with path to the data 
    /// </summary>
    /// <param name="dataPath">Path to the folder containing the VisageFaceAnalyser data file.s</param>
    bool InitializeAnalyser(string dataPath)
    {  
        int is_initialized = VisageTrackerNative._initAnalyser(dataPath);

        if ((is_initialized & (int)VFA_FLAGS.VFA_AGE) != (int)VFA_FLAGS.VFA_AGE)
        {
            return false;
        }

        if ((is_initialized & (int)VFA_FLAGS.VFA_EMOTION) != (int)VFA_FLAGS.VFA_EMOTION)
        {
            return false;
        }

        if ((is_initialized & (int)VFA_FLAGS.VFA_GENDER) != (int)VFA_FLAGS.VFA_GENDER)
        {
            return false;
        }

        return true;      
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (fa_worker.WorkerSupportsCancellation == true)
            {
                StartCoroutine(StopAgeGenderEmotionsEstimation());
            }
            Application.Quit();
        }

        if (AnalyserInitialized && Tracker.frameForAnalysis)
        {

            if (!VisageTrackerNative._prepareDataForAnalysis())
                return;

            UpdateAnalysisResults();
        }
    }

    /// <summary>
	/// Performs face analysis in a separate thread.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
    private void worker_DoWorkFaceAnalysis(object sender, DoWorkEventArgs e)
    {
        List<object> argumentList = e.Argument as List<object>;
        //read and cast the parameters
        int[] trackerStatus = (int[])argumentList[0];

        for (int i = 0; i < Tracker.MAX_FACES; i++)
        {
            if (trackerStatus[i] == (int)TrackStatus.OK)
            {
                PerformAnalysis(i);
            }
            else if (trackerStatus[i] != (int)TrackStatus.OK)
            {
                ResetAnalysis(i);
            }
        }
    }

    IEnumerator StopAgeGenderEmotionsEstimation()
    {
        if (fa_worker.IsBusy)
        {
            fa_worker.CancelAsync();
        }

        while (fa_worker.IsBusy)
        {
            yield return new WaitForSeconds(0.04f);
        }
    }

    private void UpdateAnalysisResults()
    {
        if (fa_worker != null)
        {
            if (fa_worker.IsBusy)
            {
                return;
            }
        }
        else
        {
            fa_worker = new BackgroundWorker();
            fa_worker.DoWork += worker_DoWorkFaceAnalysis;
            fa_worker.WorkerSupportsCancellation = true;
        }

        VisageTrackerNative._getTrackerStatus(TrackerStatus);

        //Start tracking from image in another thread
        List<object> arguments = new List<object>();
        arguments.Add(TrackerStatus);
        fa_worker.RunWorkerAsync(arguments);

        for (int i = 0; i < Tracker.MAX_FACES; i++)
        {
            if (TrackerStatus[i] != (int)TrackStatus.OK)
            {   
                RemoveAnalysisDataElement(i);
            }

            if (TrackerStatus[i] == (int)TrackStatus.OK)
            {           
                lock (lockAnalyser)
                {
                    DrawEmotions(i, EmotionListPerFace[i]);
                    DrawGender(i, gender[i]);
                    DrawAge(i, age[i]);
                }

                DisplayAnalysisResults(i);
            } 
        }
    }

    /// <summary>
    /// Removes element with analysis data.
    /// </summary>
    /// <param name="faceIndex">Index for the particular tracked face.</param>
    private void RemoveAnalysisDataElement(int faceIndex)
    {
          AnalysisList[faceIndex].GetComponent<MeshFilter>().transform.position -= new Vector3(0, 0, 10000);
    }


    /// <summary>
    /// Performs face analysis for given face (faceIndex).
    /// </summary>
    /// <param name="faceIndex">Index of the tracker/face for which face analysis will be performed.</param>
    private void PerformAnalysis(int faceIndex)
    {
        int analyserOptions = (int)(VFAFlags.VFA_AGE | VFAFlags.VFA_GENDER | VFAFlags.VFA_EMOTION);

        VisageTrackerNative._analyseStream(faceIndex, analyserOptions, ref analysisData[faceIndex]);

        if (!analysisData[faceIndex].ageValid || !analysisData[faceIndex].genderValid || !analysisData[faceIndex].emotionsValid)
            return;

        lock (lockAnalyser)
        {   
            age[faceIndex] = analysisData[faceIndex].age;
            gender[faceIndex] = analysisData[faceIndex].gender;
            EmotionListPerFace[faceIndex] = new float[NUM_EMOTIONS];
            EmotionListPerFace[faceIndex] = analysisData[faceIndex].emotionProbabilities;
        }
    }

    /// <summary>
    /// Resets analysis filter parameters.
    /// Typical usage is when tracker status has changed from TrackStatus.OK to TrackStatus.INIT
    /// </summary>
    /// <param name="faceIndex">Index for the particular tracked face.</param>
    private void ResetAnalysis(int faceIndex)
    {
        age[faceIndex] = -1;
        gender[faceIndex] = -1;
        EmotionListPerFace[faceIndex] = default(float[]);
        VisageTrackerNative._resetStreamAnalysis(faceIndex);
    }

    /// <summary>
    /// Adjust emotion bars according to emotion analysis values for a given tracker/face (faceIndex).
    /// If the passed Emotions array is null, no bars will be drawn.
    /// </summary>
    /// <param name="faceIndex">Index of the tracker/face for which to draw emotions</param>
    /// <param name="Emotions">Float array containing emotions</param>
    private void DrawEmotions(int faceIndex, float[] Emotions)
    {
        for (int emoIndex = 0; emoIndex < NUM_EMOTIONS; emoIndex++)
        {
            emoVertices[faceIndex][0].x = emoVertices[faceIndex][2].x;
            emoVertices[faceIndex][1].x = emoVertices[faceIndex][2].x;

            if (Emotions == null)
                return;

            float EmoBarLength = EmoBarSize * Emotions[emoIndex];
            if (Emotions[emoIndex] > EMO_DISPLAY_TRESHOLD)
            {
                emoVertices[faceIndex][0].x = emoVertices[faceIndex][2].x + EmoBarLength;
                emoVertices[faceIndex][1].x = emoVertices[faceIndex][2].x + EmoBarLength;
            }
            AnalysisList[faceIndex].GetComponentsInChildren<MeshFilter>()[3 + emoIndex].mesh.vertices = emoVertices[faceIndex];

            if (Emotions[emoIndex] > EMO_DISPLAY_TRESHOLD)
            {
                percentage[faceIndex][emoIndex].text = ((int)(Emotions[emoIndex] * 100)).ToString() + "%";
            }
            else
            {
                percentage[faceIndex][emoIndex].text = " ";
            }
        }

    }

    /// <summary>
    /// Adjust material on gender mesh according to gender analysis values for a given tracker/face (faceIndex).
    /// </summary>
    /// <param name="faceIndex">Index of the tracker/face for which to draw gemder</param>
    /// <param name="gender">0 for female, 1 for male</param>
    private void DrawGender(int faceIndex, int gender)
    {
        AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[3].enabled = true;
        if (gender == 0)
        {
            AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[3].GetComponentsInChildren<Renderer>()[0].material = matrialList[0];
            AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[3].GetComponentsInChildren<Renderer>()[0].enabled = true;
        
        }
        else if (gender == 1)
        {
            AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[3].GetComponentsInChildren<Renderer>()[0].material = matrialList[1];
            AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[3].GetComponentsInChildren<Renderer>()[0].enabled = true;
          
        }
        else
        {
            AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[3].enabled = false;
        }
    }

    /// <summary>
    /// Adjust text on age text mesh to age value for a given tracker/face (faceIndex).
    /// </summary>
    /// <param name="faceIndex">Index of the tracker/face for which to draw age</param>
    /// <param name="age">estimated age for given faceIndex</param>
    private void DrawAge(int faceIndex, float age)
    {
        if (age > 0)
        {
            AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[1].GetComponent<TextMesh>().text = (int)Math.Round(age, 0) + "y";
        }
        else
        {
            AnalysisList[faceIndex].GetComponentsInChildren<Renderer>()[1].GetComponent<TextMesh>().text = " - ";
        }
    }

    /// <summary>
    /// Calculates the chin point position and sets analysis data element at that position for a given tracker/face (faceIndex).
    /// </summary>
    /// <param name="faceIndex">Index of the tracker/face for which analysis data will be displayed</param>
    private void DisplayAnalysisResults(int faceIndex)
    {
        float[] positions = new float[3];

        VisageTrackerNative._getAllFeaturePoints3D(rawfdp, rawfdp.Length, faceIndex);

        fdpArray[faceIndex].Fill(rawfdp);

        Vector3[] positions3D = new Vector3[Tracker.MAX_FACES];

        // Get position of chin point - 2.1
        positions = fdpArray[faceIndex].getFPPos(2, 1);

        positions3D[faceIndex].x = -positions[0];
        positions3D[faceIndex].y = positions[1];
        positions3D[faceIndex].z = positions[2];

        AnalysisList[faceIndex].GetComponent<MeshFilter>().transform.position = positions3D[faceIndex];
        if ((Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) && Tracker.isMirrored == 1)
        {
            AnalysisList[faceIndex].GetComponent<MeshFilter>().transform.position = Vector3.Scale(AnalysisList[faceIndex].GetComponent<MeshFilter>().transform.position, new Vector3(-1, 1, 1));
        }
        lock (lockWithinConstraints)
        {
            if (!withinConstraints(faceIndex))
            {
                warningPanel.GetComponentInChildren<Image>().material = warningList[0];
                warningPanel.SetActive(true);
                warningPanelImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(warningPanelImage.color.a, 235 / 255f, Time.deltaTime * 3f));
            }
            else
            {
                warningPanelImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(warningPanelImage.color.a, 0.0f, Time.deltaTime * 3f));
            }
        }
    }

    /// <summary>
    /// Compares obtained rotation apparent data with set constraints if data is within the limits returns true, otherwise returns false
    /// </summary>
    /// <param name="faceIndex">Index of the tracker/face for which analysis data will be displayed</param>
    public bool withinConstraints(int faceIndex)
    {
        VisageTrackerNative._getHeadRotationApparent(rotationApparent, faceIndex);

        RotationApparent[faceIndex].x = rotationApparent[0];
        RotationApparent[faceIndex].y = rotationApparent[1];
        RotationApparent[faceIndex].z = rotationApparent[2];

        double HeadPitchCompensatedRad = RotationApparent[faceIndex].x;
        double HeadYawCompensatedRad = RotationApparent[faceIndex].y;
        double HeadRollRad = RotationApparent[faceIndex].z;
        
        double HeadPitchCompensatedDeg = HeadPitchCompensatedRad * Mathf.Rad2Deg;
        double HeadYawCompensatedDeg = HeadYawCompensatedRad * Mathf.Rad2Deg;
        double HeadRollDeg = HeadRollRad * Mathf.Rad2Deg;

        const double CONSTRAINT_ANGLE = 40;
        
        if (Math.Abs(HeadPitchCompensatedDeg) > CONSTRAINT_ANGLE ||
            Math.Abs(HeadYawCompensatedDeg) > CONSTRAINT_ANGLE ||
            Math.Abs(HeadRollDeg) > CONSTRAINT_ANGLE ||
            VisageTrackerNative._getFaceScale(faceIndex) < 40)
            return false;

        return true;
    }
#endif
}
