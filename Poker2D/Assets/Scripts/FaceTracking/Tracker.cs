using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

#if PLATFORM_ANDROID && UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif

using System.IO;
using System.Linq;

/** Class that implements the behavior of tracking application.
 * 
 * This is the core class that shows how to use visage|SDK capabilities in Unity. It connects with visage|SDK through calls to 
 * native methods that are implemented in VisageTrackerUnityPlugin.
 * It uses tracking data to transform objects that are attached to it in ControllableObjects list.
 */
public class Tracker : MonoBehaviour
{
	#region Properties
    
    #if !UNITY_WEBGL
    [HideInInspector]
    #endif
    [Header("Script path settings")]
    public string visageSDK;
    #if !UNITY_WEBGL
    [HideInInspector]
    #endif
    public string visageAnalysisData;
    #if !UNITY_WEBGL
    [HideInInspector]
    #endif
    [Header("NeuralNet configuration settings")]
    public string ConfigNeuralNet;

	[Header("Tracker configuration settings")]
	//Tracker configuration file name.
	public string ConfigFileEditor;
	public string ConfigFileStandalone;
	public string ConfigFileIOS;
	public string ConfigFileAndroid;
	public string ConfigFileOSX;
    public string ConfigFileWebGL;

	[Header("Tracking settings")]
#if UNITY_WEBGL
    public const int MAX_FACES = 1;
#else
    public const int MAX_FACES = 4;
#endif

    private bool trackerInited = false;

	[Header("Tracker output data info")]
	public Vector3[] Translation = new Vector3[MAX_FACES]; 
	public Vector3[] Rotation = new Vector3[MAX_FACES];
	private bool isTracking = false;
	public int[] TrackerStatus = new int[MAX_FACES];
    private float[] translation = new float[3];
    private float[] rotation = new float[3];
	private VsRect rectangle = new VsRect();
	private VsRect[] Rectangle = new VsRect[MAX_FACES];

    [Header("Camera settings")]
	public Material CameraViewMaterial;
	public Shader CameraViewShaderRGBA;
	public Shader CameraViewShaderBGRA;
	public Shader CameraViewShaderUnlit;
	public float CameraFocus;
	public int Orientation = 0;
	private int currentOrientation = 0;
	public int isMirrored = 1;
	private int currentMirrored = 1;
	public int camDeviceId = 0;
    private int AndroidCamDeviceId = 0;
	private int currentCamDeviceId = 0;
	public int defaultCameraWidth = -1;
	public int defaultCameraHeight = -1;
	private bool doSetupMainCamera = true;
    private bool camInited = false;

    [HideInInspector]
    public bool frameForAnalysis = false;
    public bool frameForRecog = false;
    private bool texCoordsStaticLoaded = false;

	private FaceFrameBuffer faceFrameBuffer;
	private float bufferTimeLength = 3.0f;
	private (int group, int index) referenceFeaturePointKey = (12, 1);
	private int referenceFeaturePointIndex;

	float[] landmarksDDQ;
	float[] landmarks;

	public FaceFrameBuffer FaceFrameBuffer => faceFrameBuffer;
	public int ReferenceFeaturePointIndex => referenceFeaturePointIndex;

#if UNITY_ANDROID
	private AndroidJavaObject androidCameraActivity;
	private bool AppStarted = false;
	AndroidJavaClass unity;
#endif

	#endregion

	#region Native code printing

	private bool enableNativePrinting = true;

	//For printing from native code
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void MyDelegate(string str);

	//Function that will be called from the native wrapper
	static void CallBackFunction(string str)
	{
		Debug.Log("::CallBack : " + str);
	}

#endregion

	private void Awake()
    {
#if PLATFORM_ANDROID && UNITY_2018_3_OR_NEWER
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermission(Permission.Camera);
#endif

#if UNITY_WEBGL
        VisageTrackerNative._preloadFile(Application.streamingAssetsPath + "/Visage Tracker/" + ConfigFileWebGL);
        VisageTrackerNative._preloadFile(Application.streamingAssetsPath + "/Visage Tracker/" + LicenseString.licenseString);
        VisageTrackerNative._preloadFile(Application.streamingAssetsPath + "/Visage Tracker/" + ConfigNeuralNet);
        VisageTrackerNative._setDataPath(".");

        VisageTrackerNative._preloadExternalJS(visageAnalysisData);
        VisageTrackerNative._preloadExternalJS(visageSDK);
        
#endif

        // Set callback for printing from native code
        if (enableNativePrinting)
         {
         /*  MyDelegate callback_delegate = new MyDelegate(CallBackFunction);
             // Convert callback_delegate into a function pointer that can be
             // used in unmanaged code.
             IntPtr intptr_delegate = Marshal.GetFunctionPointerForDelegate(callback_delegate);
             // Call the API passing along the function pointer.
             VisageTrackerNative.SetDebugFunction(intptr_delegate);*/
         }


#if UNITY_ANDROID
		Unzip();
#endif

        string licenseFilePath = Application.streamingAssetsPath + "/" + "/Visage Tracker/";

		// Set license path depending on platform
		switch (Application.platform)
		{

			case RuntimePlatform.IPhonePlayer:
				licenseFilePath = "Data/Raw/Visage Tracker/";
				break;
			case RuntimePlatform.Android:
				licenseFilePath = Application.persistentDataPath + "/";
				break;
			case RuntimePlatform.OSXPlayer:
				licenseFilePath = Application.dataPath + "/Resources/Data/StreamingAssets/Visage Tracker/";
				break;
			case RuntimePlatform.OSXEditor:
				licenseFilePath = Application.dataPath + "/StreamingAssets/Visage Tracker/";
				break;
            case RuntimePlatform.WebGLPlayer:
                licenseFilePath = "";
                break;
			case RuntimePlatform.WindowsEditor:
                licenseFilePath = Application.streamingAssetsPath + "/Visage Tracker/";
				break;
		}


#if UNITY_STANDALONE_WIN
		//NOTE: licensing for Windows platform expects folder path exclusively
		VisageTrackerNative._initializeLicense(licenseFilePath);
#else
		//NOTE: platforms other than Windows expect absolute or relative path to the license file
		VisageTrackerNative._initializeLicense(licenseFilePath + LicenseString.licenseString);
#endif

	}


	void Start()
	{
		// Set configuration file path and name depending on a platform
		string configFilePath = Application.streamingAssetsPath + "/" + ConfigFileStandalone;

        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
                configFilePath = "Data/Raw/Visage Tracker/" + ConfigFileIOS;
                break;
            case RuntimePlatform.Android:
                configFilePath = Application.persistentDataPath + "/" + ConfigFileAndroid;
                break;
            case RuntimePlatform.OSXPlayer:
                configFilePath = Application.dataPath + "/Resources/Data/StreamingAssets/Visage Tracker/" + ConfigFileOSX;
                break;
            case RuntimePlatform.OSXEditor:
                configFilePath = Application.dataPath + "/StreamingAssets/Visage Tracker/" + ConfigFileOSX;
                break;
            case RuntimePlatform.WebGLPlayer:
                configFilePath = ConfigFileWebGL;
                break;
            case RuntimePlatform.WindowsEditor:
                configFilePath = Application.streamingAssetsPath + "/" + ConfigFileEditor;
                break;
        }

        // Initialize tracker with configuration and MAX_FACES
        trackerInited = InitializeTracker(configFilePath);

		// Get current device orientation
		Orientation = GetDeviceOrientation();

		// Open camera in native code
		camInited = OpenCamera(Orientation, camDeviceId, defaultCameraWidth, defaultCameraHeight, isMirrored);

		// Initialize buffer for storing facial frames
		faceFrameBuffer = new FaceFrameBuffer(bufferTimeLength);

		// Find the index of the reference FP inside a flattened array
		int fpStartGroupIndex = VisageTrackerNative._getFP_START_GROUP_INDEX();
		int fpEndGroupIndex = VisageTrackerNative._getFP_END_GROUP_INDEX();
		int length = fpEndGroupIndex - fpStartGroupIndex + 1;
		int[] groupSizes = new int[length];
		VisageTrackerNative._getGroupSizes(groupSizes, length);
		referenceFeaturePointIndex = groupSizes[..(referenceFeaturePointKey.group - 2)].Sum();
		Debug.Log("INDEX: " + referenceFeaturePointIndex);

		// Create array to load landmarks with deined detected and quality flags
		//		dimension of array -> number of landmarks * 6 (for x, y and z coordinates and defined, detected and quality flags)
		landmarksDDQ = new float[groupSizes.Sum() * 6];

		// Create array to store landmark coordinates
		//		dimension of array -> number of landmarks * 3 (for x, y and z coordinates)
		landmarks = new float[groupSizes.Sum() * 3];
	}


    void Update()
	{
        //signals analysis and recognition to stop if camera or tracker are not initialized and until new frame and tracking data are obtained
        frameForAnalysis = false;
        frameForRecog = false;

        if (!isTrackerReady())
            return;

#if (UNITY_IPHONE || UNITY_ANDROID) && UNITY_EDITOR
		// tracking will not work if the target is set to Android or iOS while in editor
		return;
#endif

        if (isTracking)
		{
#if UNITY_ANDROID
			if (VisageTrackerNative._frameChanged())
			{
				texture = null;
				doSetupMainCamera = true;
			}	
#endif
            Orientation = GetDeviceOrientation();

            // Check if orientation or camera device changed
            if (currentOrientation != Orientation || currentCamDeviceId != camDeviceId || currentMirrored != isMirrored)
			{
                currentCamDeviceId = camDeviceId;
                currentOrientation = Orientation;
                currentMirrored = isMirrored;

                // Reopen camera with new parameters 
                OpenCamera(currentOrientation, currentCamDeviceId, defaultCameraWidth, defaultCameraHeight, currentMirrored);
				doSetupMainCamera = true;

			}

            // grab current frame and start face tracking
            VisageTrackerNative._grabFrame();

			VisageTrackerNative._track();
            VisageTrackerNative._getTrackerStatus(TrackerStatus);

            //After the track has been preformed on the new frame, the flags for the analysis and recognition are set to true
            frameForAnalysis = true;
            frameForRecog = true;
			
			// Get only for the first face: presumed to be the player's landmarks with defined, detected and quality flags: 
			VisageTrackerNative._getAllFeaturePoints3D(landmarksDDQ, landmarksDDQ.Length, 0);

			// Remove the defined, detected and quality flags to keep only raw coordinates of each facial feature point
			landmarks = ExtractXYZ(landmarksDDQ);

			Debug.Log($"Landmarks: [{String.Join(", ", landmarks.Select(v => v.ToString()))}]");

			// Fill the frame buffer with face frame data
			if (faceFrameBuffer != null && landmarks != null && landmarks.Length > 0)
			{
				float[] landmarksSnapshot = new float[landmarks.Length];
				Array.Copy(landmarks, landmarksSnapshot, landmarks.Length);
				faceFrameBuffer.AddFrame(new FaceFrame(Time.time, landmarksSnapshot));
			}
		}

	}

	public static float[] ExtractXYZ(float[] input)
	{
		int inputStride = 6;
		int outputStride = 3;

		int pointCount = input.Length / inputStride;
		float[] output = new float[pointCount * outputStride];

		int inIdx = 0;
		int outIdx = 0;

		for (int i = 0; i < pointCount; i++)
		{
			output[outIdx] = input[inIdx];     // x
			output[outIdx + 1] = input[inIdx + 1]; // y
			output[outIdx + 2] = input[inIdx + 2]; // z

			inIdx += inputStride;
			outIdx += outputStride;
		}

		return output;
	}

	bool isTrackerReady()
    {
        if (camInited && trackerInited)
        {    
            isTracking = true;
        }
        else
        {
            isTracking = false;
        }
        return isTracking;
    }

    void OnDestroy()
	{
#if UNITY_ANDROID
		this.androidCameraActivity.Call("closeCamera");
#else
		camInited = !(VisageTrackerNative._closeCamera());
#endif
	}

#if UNITY_IPHONE
	void OnApplicationPause(bool pauseStatus) {
		if(pauseStatus){
			camInited = !(VisageTrackerNative._closeCamera());
			isTracking = false;
		}
		else
		{
			camInited = OpenCamera(Orientation, camDeviceId, defaultCameraWidth, defaultCameraHeight, isMirrored);
			isTracking = true;
		}
	}
#endif


	/// <summary>
	/// Initialize tracker with maximum number of faces - MAX_FACES.
	/// Additionally, depending on a platform set an appropriate shader.
	/// </summary>
	/// <param name="config">Tracker configuration path and name.</param>
	bool InitializeTracker(string config)
	{
		Debug.Log("Visage Tracker: Initializing tracker with config: '" + config + "'");

#if (UNITY_IPHONE || UNITY_ANDROID) && UNITY_EDITOR
		return false;
#endif

#if UNITY_ANDROID

		Shader shader = Shader.Find("Unlit/Texture");
		CameraViewMaterial.shader = shader;

		// initialize visage vision
		VisageTrackerNative._loadVisageVision();

		unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		this.androidCameraActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
#elif UNITY_STANDALONE_WIN || UNITY_WEBGL
		Shader shader = Shader.Find("Custom/RGBATex");
		CameraViewMaterial.shader = shader;
#else
        Shader shader = Shader.Find("Custom/BGRATex");
		CameraViewMaterial.shader = shader;
#endif

#if UNITY_WEBGL
        // initialize tracker
        VisageTrackerNative._initTracker(config, MAX_FACES, "CallbackInitTracker");
        return trackerInited;
#else
        VisageTrackerNative._initTracker(config, MAX_FACES);      
        return true;
#endif
    }

#region Callback Function for WEBGL

    public void CallbackInitTracker()
    {
        Debug.Log("TrackerInited");
        trackerInited = true;
    }

    public void OnSuccessCallbackCamera()
    {
        Debug.Log("CameraSuccess");
        camInited = true;
    }

    public void OnErrorCallbackCamera()
    {
        Debug.Log("CameraError");
    }

#endregion


	/// <summary>
	/// Get current device orientation.
	/// </summary>
	/// <returns>Returns an integer:
	/// <list type="bullet">
	/// <item><term>0 : DeviceOrientation.Portrait</term></item>
	/// <item><term>1 : DeviceOrientation.LandscapeRight</term></item>
	/// <item><term>2 : DeviceOrientation.PortraitUpsideDown</term></item>
	/// <item><term>3 : DeviceOrientation.LandscapeLeft</term></item>
	/// </list>
	/// </returns>
	int GetDeviceOrientation()
	{
		int devOrientation;

#if UNITY_ANDROID
		//Device orientation is obtained in AndroidCameraPlugin so we only need information about whether orientation is changed
		int oldWidth = ImageWidth;
		int oldHeight = ImageHeight;

		VisageTrackerNative._getCameraInfo(out CameraFocus, out ImageWidth, out ImageHeight);

		if ((oldWidth!=ImageWidth || oldHeight!=ImageHeight) && ImageWidth != 0 && ImageHeight !=0 && oldWidth != 0 && oldHeight !=0 )
			devOrientation = (Orientation ==1) ? 0:1;
		else
			devOrientation = Orientation;
#else
		if (Input.deviceOrientation == DeviceOrientation.Portrait)
			devOrientation = 0;
		else if (Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
			devOrientation = 2;
		else if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
			devOrientation = 3;
		else if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
			devOrientation = 1;
		else if (Input.deviceOrientation == DeviceOrientation.FaceUp)
			devOrientation = Orientation;
        else if (Input.deviceOrientation == DeviceOrientation.Unknown)
            devOrientation = Orientation;
        else
			devOrientation = 0;
#endif

		return devOrientation;
	}


	/// <summary> 
	/// Open camera from native code. 
	/// </summary>
	/// <param name="orientation">Current device orientation:
	/// <list type="bullet">
	/// <item><term>0 : DeviceOrientation.Portrait</term></item>
	/// <item><term>1 : DeviceOrientation.LandscapeRight</term></item>
	/// <item><term>2 : DeviceOrientation.PortraitUpsideDown</term></item>
	/// <item><term>3 : DeviceOrientation.LandscapeLeft</term></item>
	/// </list>
	/// </param>
	/// <param name="camDeviceId">ID of the camera device.</param>
	/// <param name="width">Desired width in pixels (pass -1 for default 800).</param>
	/// <param name="height">Desired width in pixels (pass -1 for default 600).</param>
	/// <param name="isMirrored">true if frame is to be mirrored, false otherwise.</param>
	bool OpenCamera(int orientation, int cameraDeviceId, int width, int height, int isMirrored)
	{
#if UNITY_ANDROID
		if (cameraDeviceId == AndroidCamDeviceId && AppStarted)
			return false;

        AndroidCamDeviceId = cameraDeviceId;
		//camera needs to be opened on main thread
		this.androidCameraActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
			this.androidCameraActivity.Call("closeCamera");
			this.androidCameraActivity.Call("GrabFromCamera", width, height, camDeviceId);
		}));
		AppStarted = true;
		return true;
#elif UNITY_WEBGL
        VisageTrackerNative._openCamera(ImageWidth, ImageHeight, isMirrored, "OnSuccessCallbackCamera", "OnErrorCallbackCamera");
        return false;
#elif UNITY_STANDALONE_WIN
        VisageTrackerNative._openCamera(orientation, cameraDeviceId, width, height); 
        return true;
#else
        VisageTrackerNative._openCamera(orientation, cameraDeviceId, width, height, isMirrored); 
        return true;
#endif
	}


#if UNITY_ANDROID
	void Unzip()
	{
		string[] pathsNeeded = {
			"vft/fm/candide3.fdp",
			"vft/fm/candide3.wfm",
			"vft/fm/jk_300.fdp",
			"vft/fm/jk_300.wfm",
			"vft/fm/jk_300.cfg",
			"Head Tracker.cfg",
			"Facial Features Tracker.cfg",
			"NeuralNet.cfg",
			"vft/ff/ff.tflite",
			"vfa/ad/ae.tflite",
			"vfa/ed/ed0.lbf",
			"vfa/ed/ed1.lbf",
			"vfa/ed/ed2.lbf",
			"vfa/ed/ed3.lbf",
			"vfa/ed/ed4.lbf",
			"vfa/ed/ed5.lbf",
			"vfa/ed/ed6.lbf",
			"vfa/gd/gd.tflite",
			"vft/er/efa.lbf",
			"vft/er/efc.lbf",
			"vfr/fr.tflite",
			"vft/pr/pr.tflite",
			"vft/fa/aux_file.bin",
			"vft/fa/d1qy.tflite",
			"vft/fa/d2.tflite",
			"license-file-name.vlc"
		};
		string outputDir;
		string localDataFolder = "Visage Tracker";

		outputDir = Application.persistentDataPath;

		if (!Directory.Exists(outputDir))
		{
			Directory.CreateDirectory(outputDir);
		}
		foreach (string filename in pathsNeeded)
		{
			WWW unpacker = new WWW("jar:file://" + Application.dataPath + "!/assets/" + localDataFolder + "/" + filename);

			while (!unpacker.isDone) { }

			if (!string.IsNullOrEmpty(unpacker.error))
			{
				continue;
			}

			if (filename.Contains("/"))
			{
				string[] split = filename.Split('/');
				string name = "";
				string folder = "";
				string curDir = outputDir;

				for (int i = 0; i < split.Length; i++)
				{
					if (i == split.Length - 1)
					{
						name = split[i];
					}
					else
					{
						folder = split[i];
						curDir = curDir + "/" + folder;
					}
				}
				if (!Directory.Exists(curDir))
				{
					Directory.CreateDirectory(curDir);
				}

				File.WriteAllBytes("/" + curDir + "/" + name, unpacker.bytes);
			}
			else
			{
				File.WriteAllBytes("/" + outputDir + "/" + filename, unpacker.bytes);
			}
		}
	}
#endif
}
