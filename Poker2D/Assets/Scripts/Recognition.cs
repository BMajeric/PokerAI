using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.UI;


/** Class that implements the behaviour of tracking application.
 * 
 * This is the core class that shows how to use visage|SDK capabilities in Unity. It connects with visage|SDK through calls to native methods that are implemented in VisageTrackerUnityPlugin.
 * It uses tracking data to transform objects that are attached to it in ControllableObjects list.
 */
public class Recognition : MonoBehaviour
{
#if !UNITY_WEBGL
    #region Properties

    [Header("Face recognition data folder settings")]
	public string dataRFolderAndroid;
	public string dataRFolderEditor;
	public string dataRFolderStandalone;

#if !UNITY_STANDALONE_OSX && !UNITY_ANDROID && !UNITY_IPHONE
	[HideInInspector]
#endif

	[Header("Tracker object settings")]
	public Tracker Tracker;
    private Vector3[] RotationApparent = new Vector3[Tracker.MAX_FACES];
    private float[] rotationApparent = new float[3];

    //Button
    private Sprite recogStart;
	private Sprite recogStop;
	[Header("GUI button settings")]
	public Button recognitionButton;

    //
	const int MAX_NAMES = 10;                   // max number of GUI elements in the gallery
#if UNITY_ANDROID
    const int NEW_IDENTITIES_NEEDED = 2;       // number of frames needed to decide if new identity is present
#else
    const int NEW_IDENTITIES_NEEDED = 20;       // number of frames needed to decide if new identity is present
#endif
	const int NUM_INIT_FRAMES = 2;             // number of frames needed before new identity is added to the gallery
	const float SIMILARITY_THRESHOLD = 0.65f;   // threshold above which two faces are deemed similar

	int[] numInitFrames = new int[Tracker.MAX_FACES];
	int[] numFramesNewIdentity = new int[Tracker.MAX_FACES];
	string[] currentDisplayName = new string[Tracker.MAX_FACES];

	List<string> UniqueNames = new List<string>();
	Dictionary<int, string> indicesToNames = new Dictionary<int, string>();
	Dictionary<int, List<short[]>> descBuffer = new Dictionary<int, List<short[]>>();

	volatile int numPersons = 0;                // number of identities added to FR API gallery
	int numPersonsAdded = 0;                    // number of identities added to GUI gallery
    //

	[Header("GUI gallery")]
	public List<GameObject> recognitionGallery = new List<GameObject>();
	public GameObject galleryPanel;
	public GameObject galleryPanelElement;

	[Header("Identity display")]
	public GameObject identity;
	public List<GameObject> identityList = new List<GameObject>();

	//Background
	private BackgroundWorker fr_worker;
	private System.Object lockRecognition = new System.Object();

	//Control variables
	bool isRecognitionInitialized = false;
	volatile bool isRecognitionOn = false;

    #endregion


	/// <summary>
	/// Wait for background thread to finish before quitting.
	/// </summary>
	private void OnApplicationQuit()
	{
		if (fr_worker.WorkerSupportsCancellation == true && fr_worker.IsBusy)
		{
			StartCoroutine(StopRecognition());
		}
	}


	void Start ()
	{
		string dataPathR = Application.streamingAssetsPath + "/" + dataRFolderStandalone;

		// Set up path to data needed by FaceRecognition
		switch (Application.platform)
		{
			case RuntimePlatform.IPhonePlayer:
			   
				break;
			case RuntimePlatform.Android:
			dataPathR = Application.persistentDataPath + "/" + dataRFolderAndroid;

				break;
			case RuntimePlatform.OSXPlayer:
	  
				break;
			case RuntimePlatform.OSXEditor:
			   
				break;
			case RuntimePlatform.WindowsEditor:
			   
				dataPathR = Application.streamingAssetsPath + "/" + dataRFolderEditor;
				break;
		}

		// Set up BackgroundWorker
		fr_worker = new BackgroundWorker();
		fr_worker.DoWork += new DoWorkEventHandler(worker_DoWorkExtractDescriptor);
		fr_worker.WorkerSupportsCancellation = true;
	   
		// Initialize FaceRecognition
		isRecognitionInitialized = VisageTrackerNative._initRecognition(dataPathR);

		if (isRecognitionInitialized)
		{
			// Intialize arrays used for face recognition
			InitializeContainers();
		}

        recognitionButton.gameObject.SetActive(true);

        LoadButtonSprites();

        galleryPanel.SetActive(false);
	}


	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			isRecognitionOn = false;
			if (fr_worker.WorkerSupportsCancellation == true)
			{
				StartCoroutine(StopRecognition());
			}
			Application.Quit();
		}

		int[] TrackerStatus = new int[Tracker.MAX_FACES];

		// Get current track status
		VisageTrackerNative._getTrackerStatus(TrackerStatus);
		
		for (int faceIndex = 0; faceIndex < Tracker.MAX_FACES; ++faceIndex)
		{
			// Recognition is started and tracker status for the particular face is OK 
			if (isRecognitionOn && TrackerStatus[faceIndex] == (int)TrackStatus.OK)
			{
				int[] groups = new int[] { 2 };
				int[] indices = new int[] { 1 };
				int[] defined = new int[1];
                int[] detected = new int[1];
                float[] quality = new float[1];
                float[] positions = new float[3];
        
                // Get position of chin point - 2.1
                //bool success = VisageTrackerNative._getFeaturePoints3D(1, groups, indices, positions, defined, faceIndex);
                VisageTrackerNative._getFeaturePoints3D(1, groups, indices, positions, defined, detected, quality, faceIndex);            

                Vector3[] positions3D = new Vector3[MAX_NAMES];
				
                if ((Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) && Tracker.isMirrored == 1)
                    positions3D[faceIndex].x = positions[0] + 0.01f;
                else
                    positions3D[faceIndex].x = -positions[0] + 0.01f;
                
                positions3D[faceIndex].y = positions[1] - 0.01f;
                positions3D[faceIndex].z = positions[2];

				// Set position of input element on the tip of chin
				identityList[faceIndex].GetComponent<TextMesh>().transform.position = positions3D[faceIndex];
				identityList[faceIndex].GetComponent<TextMesh>().text = currentDisplayName[faceIndex];

				// Sync with the background thread due to numPersons variable
				lock (lockRecognition)
				{
					// Add recognized identities to the GUI gallery (max gallery items is 10)
					if (numPersonsAdded < numPersons && recognitionGallery.Count < 10)
					{
						ShowInGallery(numPersonsAdded);
						numPersonsAdded++;
					}
				}
			}
			// Recognition is started but tracker status for the particular face is not OK 
			else if (isRecognitionOn && TrackerStatus[faceIndex] != (int)TrackStatus.OK)
			{
				identityList[faceIndex].GetComponent<TextMesh>().transform.position -= new Vector3(0, 0, 10000);
				identityList[faceIndex].GetComponent<TextMesh>().text = " ";
			}
		}
	}


#region GUI Buttons OnClick events

	public void onButtonRecognition()
	{

        if (!isRecognitionInitialized)
            return;
       
		if (!isRecognitionOn)
		{
			recognitionButton.image.overrideSprite = recogStop;

			// Show gallery panel
			galleryPanel.SetActive(true);
			   
			for (int i = 0; i < Tracker.MAX_FACES; ++i)
			{
                // For each face create a GUI element used to display the name
                identityList.Add((GameObject)Instantiate(identity));
            }

			isRecognitionOn = true;

			//run backgroundworker
			fr_worker.RunWorkerAsync();
			
		}
		else
		{
			recognitionButton.image.overrideSprite = recogStart;

			// Hide gallery panel
			galleryPanel.SetActive(false);

			isRecognitionOn = false;

			// Stop recognition thread and clear variables
			StartCoroutine(StopRecognition());
		}
	}


	private void StopButtonClick()
	{
		if (fr_worker.WorkerSupportsCancellation == true)
		{
			StartCoroutine(StopRecognition());
		}

	}

#endregion


	/// <summary>
	/// Propagate changes from the GUI gallery to the FR API gallery.
	/// </summary>
	/// <param name="name">New name</param>
	/// <param name="galleryIndex">Index of the name in the gallery</param>
	void OnNameChanged(string name, int galleryIndex)
	{
		// Find the current name by galleryIndex
		string current_name = indicesToNames[galleryIndex];

		// Iterate over FR API gallery and replace each occurence of the currentName with the new name
		for (int i = 0; i < VisageTrackerNative._getDescriptorCount(); i++)
		{
			if (VisageTrackerNative._getDescriptorName(i) == current_name)
				VisageTrackerNative._replaceDescriptorName(name, i);
		}

		// Update UniqueNames array
		for (int i = 0; i < UniqueNames.Count; i++)
		  if (UniqueNames[i] == current_name)
			  UniqueNames[i] = name;

		// Update indicesToNames array
		indicesToNames[galleryIndex] = name;
	}


	/// <summary>
	/// Stop FaceRecognition and clear variables with regard to background thread.
	/// </summary>
	/// <returns></returns>
	IEnumerator StopRecognition()
	{
		isRecognitionOn = false;

		if (fr_worker.IsBusy)
		{
			fr_worker.CancelAsync();
		}

		while (fr_worker.IsBusy)
		{
			yield return new WaitForSeconds(0.04f);
		}

		VisageTrackerNative._resetGallery();

		numPersons = 0;
		numPersonsAdded = 0;
		UniqueNames.Clear();
		indicesToNames = new Dictionary<int, string>();
		for (int i = 0; i < Tracker.MAX_FACES; ++i)
		{
			currentDisplayName[i] = "";
			descBuffer[i].Clear();
			numInitFrames[i] = 0;
			numFramesNewIdentity[i] = 0;
		}

		for (int i = 0; i < identityList.Count; ++i)
		{
			DestroyImmediate(identityList[i]);
		}
		identityList.Clear();

		for (int i = 0; i < recognitionGallery.Count; ++i)
		{
			DestroyImmediate(recognitionGallery[i]);
		}
		recognitionGallery.Clear();
	}


	/// <summary>
	/// Performs face recognition on a single face.
	/// </summary>
	/// <param name="faceIndex">Index of the face for which face recognition will be performed</param>
	private void RecognizeFace(int faceIndex)
	{
		// Get descriptor length
		int DESCRIPTOR_SIZE = VisageTrackerNative._getDescriptorSize();
		short[] descriptor = new short[DESCRIPTOR_SIZE];

		// Number of collected frames is under the threshold, collect more
		if (numInitFrames[faceIndex] < NUM_INIT_FRAMES)
		{
			currentDisplayName[faceIndex] = "?";

			// Extract descriptor
			int extract_descriptor_status = VisageTrackerNative._extractDescriptor(descriptor, faceIndex);
			
			// Extraction was successful
			if (extract_descriptor_status == 1)
			{
				//Add descriptor to the buffer for face index
				descBuffer[faceIndex].Add(descriptor);

				numInitFrames[faceIndex] += 1;

				//Check if sufficient number of face descriptors has been collected if it has add them all to the gallery with the unique ID - Person #
				if (numInitFrames[faceIndex] == NUM_INIT_FRAMES)
				{
					for (int i = 0; i < NUM_INIT_FRAMES; i++)
					{
						VisageTrackerNative._addDescriptor(descBuffer[faceIndex][i], "Person" + numPersons);
					}

					//Clear buffer after descriptors have been added to the gallery
					descBuffer[faceIndex].Clear();
					lock (lockRecognition)
					{
						UniqueNames.Add("Person" + numPersons);
						indicesToNames[numPersons] = "Person" + numPersons;
						numPersons += 1;
					}
				}
			}	

		}

		//Check if the initialization phase is complete
		else
		{
            int count = VisageTrackerNative._getDescriptorCount();
			float[] sim = new float[count];
			List<string> names = new List<string>();

			//Extract a face descriptor from the current face
			int extract_descriptor_status = VisageTrackerNative._extractDescriptor(descriptor, faceIndex);     

			//Compare the descriptor to all the descriptors in the gallery (count)
			int recognize_status = VisageTrackerNative._recognizeWrapper(descriptor, count, names, sim);
			
			//If face recognition was successful and the recognized face is recognized with high enough similarity, display the name
			if (recognize_status > 0 && extract_descriptor_status == 1 && sim[0] > SIMILARITY_THRESHOLD)
			{
				currentDisplayName[faceIndex] = names[0];
				numFramesNewIdentity[faceIndex] = 0;
			}
			//Case were extract descriptor fails, for example when on edge of the screen, keep the name and do not collect new descriptors
			else if (extract_descriptor_status == 0)
			{
				numFramesNewIdentity[faceIndex] = 0;
			}
			//Otherwise keep count on how many frames in a row similarity has not been high enough
			//If the number is above threshold we conclude that there is a new face in the frame and we go to the initialisation phase again
			else
			{
				if (numFramesNewIdentity[faceIndex] / (float)NEW_IDENTITIES_NEEDED > 0)
					currentDisplayName[faceIndex] = "?";
				else if (recognize_status > 0 && sim[0] > SIMILARITY_THRESHOLD)
					currentDisplayName[faceIndex] = names[0] + " "  + sim[0];
				else
					currentDisplayName[faceIndex] = "?";

				numFramesNewIdentity[faceIndex] += 1;

				if (numFramesNewIdentity[faceIndex] > NEW_IDENTITIES_NEEDED)
				{
					numInitFrames[faceIndex] = 0;
					numFramesNewIdentity[faceIndex] = 0;
				}
			}
		}
	}


	/// <summary>
	/// Performs face recognition in a separate thread.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void worker_DoWorkExtractDescriptor(object sender, DoWorkEventArgs e)
	{    
		if (!isRecognitionInitialized)
			return;

		int[] TrackerStatusRec = new int[Tracker.MAX_FACES];

		// Check if first tracker status is OFF - that indicates that the tracking is not working
		bool isTrackerOFF = Tracker.TrackerStatus[0] == (int)TrackStatus.OFF;

		while (!isTrackerOFF && isRecognitionOn)
		{
			// If worker is not being canceled and there is data ready for recognition
			if (!fr_worker.CancellationPending && Tracker.frameForRecog)
			{
				// 
				VisageTrackerNative._prepareDataForRecog();
				VisageTrackerNative._getTrackerStatus(TrackerStatusRec);

				for (int faceIndex = 0; faceIndex < TrackerStatusRec.Length; ++faceIndex)
				{
					if (TrackerStatusRec[faceIndex] == (int)TrackStatus.OK && withinConstraints(faceIndex))
					{
						RecognizeFace(faceIndex);
					}
					else
					{
						ResetFRInitialization(faceIndex);
					}
				}           
			}
		}
		e.Cancel = true;
	}


	/// <summary>
	/// Resets face recognition for a single face or for all faces.
	/// </summary>
	/// <param name="index">Index of the face. If nothing is passed resets recognition for all faces.</param>
	private void ResetFRInitialization(int index = -1)
	{
		if (index == -1)
		{
			for (int faceIndex = 0; faceIndex < Tracker.MAX_FACES; ++faceIndex)
			{
				descBuffer[faceIndex].Clear();
				numFramesNewIdentity[faceIndex] = 0;

				if (numInitFrames[faceIndex] != 0 && numInitFrames[faceIndex] < NUM_INIT_FRAMES)
					numInitFrames[faceIndex] = 0;
			}
		}
		else
		{
			descBuffer[index].Clear();
			numFramesNewIdentity[index] = 0;

			if (numInitFrames[index] != 0 && numInitFrames[index] < NUM_INIT_FRAMES)
				numInitFrames[index] = 0;
		}
	}


	/// <summary>
	/// Displays names from the FR API gallery in GUI galleryPanel in Unity
	/// </summary>
	/// <param name="index">Index of the element in GUI gallery</param>
	void ShowInGallery(int index)
	{   
		recognitionGallery.Add((GameObject)Instantiate(galleryPanelElement, galleryPanel.transform));
		recognitionGallery[index].GetComponent<InputField>().text = UniqueNames[index];
		recognitionGallery[index].GetComponent<InputField>().onValueChanged.AddListener(delegate { OnNameChanged(recognitionGallery[index].GetComponent<InputField>().text, index); });
	}


	/// <summary>
	/// Initialize arrays used for face recognition.
	/// </summary>
	void InitializeContainers()
	{
		for (int faceIndex = 0; faceIndex < Tracker.MAX_FACES; ++faceIndex)
		{
			//For each face, initialize a list of face descriptors
			descBuffer.Add(faceIndex, new List<short[]>());
			//For each face reset the counters
			numInitFrames[faceIndex] = 0;
			numFramesNewIdentity[faceIndex] = 0;
			//
			currentDisplayName[faceIndex] = "";
		}
	}


	/// <summary>
	/// Load sprites for GUI recognition button
	/// </summary>
	private void LoadButtonSprites()
	{
        recogStart = Resources.Load<Sprite>("face recognition");
		recogStop = Resources.Load<Sprite>("no face recognition");
	}


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
