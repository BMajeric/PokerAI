using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

public enum VFA_FLAGS
{
    VFA_AGE = 1,
    VFA_GENDER = 2,
    VFA_EMOTION = 4
}

public enum TrackStatus
{
    OFF = 0,
    OK = 1,
    RECOVERING = 2,
    INIT = 3
}

public static partial class VisageTrackerNative
{

#if UNITY_STANDALONE_WIN

#if (UNITY_64 || UNITY_EDITOR_64)
    const string dllName = "VisageTrackerUnityPlugin64";
#else
    const string dllName = "VisageTrackerUnityPlugin";
#endif

    [DllImport(dllName)]
    public static extern void SetDebugFunction(IntPtr fp);

    #region Tracker

    /** This function initialises the license.
 	 * 
 	 * Implemented in VisageTrackerUnityPlugin library.
 	 */
    [DllImport(dllName)]
    public static extern void _initializeLicense(string license);


    /**This function grabs current frame.
    * 
    * Implemented in VisageTrackerUnity library.
    */
    [DllImport(dllName)]
    public static extern void _grabFrame();


    /** Fills the imageData with the current frame image data.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _setFrameData(IntPtr texIDPointer);


    /** This function initializes new camera with the given orientation and camera information.
     * 
     * Implemented in VisageTrackerUnityPlugin library.
     */
    [DllImport(dllName)]
    public static extern void _openCamera(int orientation, int deviceID, int width, int height);


    /** This function closes camera.
     * 
     * Implemented in VisageTrackerUnityPlugin library.
     */
    [DllImport(dllName)]
    public static extern bool _closeCamera();

    /** This function initialises the tracker.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _initTracker(string config, int numFaces);


    /** This function releases memory allocated by the tracker.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _releaseTracker();


    /** This function starts face tracking on current frame.
    * 
    * Implemented in VisageTrackerUnity library.
    */
    [DllImport(dllName)]
    public static extern void _track();


    /** This function returns array of tracking statuses for each of the tracked faces.
     * 
     * Implemented in VisageTrackerUnity library.
     */
    [DllImport(dllName)]
    public static extern void _getTrackerStatus(int[] tStatus);


    /** This functions returns camera info.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getCameraInfo(out float focus, out int width, out int height);

    /** This functions returns static texture coordinates of the mesh.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getTexCoordsStatic(float[] buffer, out int texCoordNumber);

    /** This functions returns estimated tracking quality level for the current frame. The value is between 0 and 1.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern float _getTrackingQuality(int faceIndex);

    /** This functions returns the current head translation.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getHeadTranslation(float[] translation, int faceIndex);

    /** This functions returns the current head rotation.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getHeadRotation(float[] rotation, int faceIndex);

    /** This functions returns the current apparent head rotation.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getHeadRotationApparent(float[] rotationApparent, int faceIndex);

    /** This function returns number of vertices in the 3D face model.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getFaceModelVertexCount();

    /** This function returns vertex coordinates.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFaceModelVertices(float[] vertices, int faceIndex);

    /** This function returns  projected (image space) vertex coordinates.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFaceModelVerticesProjected(float[] verticesProjected, int faceIndex);

    /** Returns number of triangles in the 3D face model. 
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getFaceModelTriangleCount();

    /** This function returns triangles coordinates.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFaceModelTriangles(int[] triangles, int faceIndex);

    /** Returns texture coordinates.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFaceModelTextureCoords(float[] texCoord, int faceIndex);

    /** This function returns global 3D feature point positions.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getAllFeaturePoints3D(float[] byteOffset, int length, int faceIndex);

    /** Returns global 3D feature point positions.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFeaturePoints3D(int N, int[] groups, int[] indices, float[] positions3D, int[] defined, int[] detected, float[] quality, int faceIndex);

    /** This function returns the feature points positions in normalized 2D screen coordinates.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getAllFeaturePoints2D(float[] byteOffset, int length, int faceIndex);

    /** This function returns the feature points positions in normalized 2D screen coordinates.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFeaturePoints2D(int N, int[] groups, int[] indices, float[] positions3D, int[] defined, int[] detected, float[] quality, int faceIndex);

    /** This function returns the 3D coordinates relative to the face origin, placed at the center between eyes.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getAllFeaturePoints3DRelative(float[] byteOffset, int length, int faceIndex);

    /** This function returns the 3D coordinates relative to the face origin, placed at the center between eyes.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFeaturePoints3DRelative(int N, int[] groups, int[] indices, float[] positions3D, int[] defined, int[] detected, float[] quality, int faceIndex);

    /** This function returns index of the first feature point group.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getFP_START_GROUP_INDEX();

    /** This function returns index of the last feature point group.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getFP_END_GROUP_INDEX();

    /** Returns the number of feature points per group.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
     [DllImport(dllName)]
    public static extern void _getGroupSizes(int[] byteOffset, int length);

    /** This function returns the frame rate of the tracker, in frames per second, measured over last 10 frames.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern float _getFrameRate();


    /** This function returns timestamp of the current video frame.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getTimeStamp();


    /** This function returns scale in pixels of facial bounding box for the given faceIndex. 
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getFaceScale(int faceIndex);


    /** This function returns facial bounding box for the given faceIndex.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getFaceBoundingBox(ref VsRect rectangle, int faceIndex);


    /** This function returns the gaze direction for the given faceIndex.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern bool _getGazeDirection(float[] direction, int faceIndex);


    /** This function returns the global gaze direction for the given faceIndex.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern bool _getGazeDirectionGlobal(float[] direction, int faceIndex);


    /** This function returns eye closure value for the given faceindex.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern bool _getEyeClosure(float[] closure, int faceIndex);

    /** This function returns iris radius values for the given faceIndex.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern bool _getIrisRadius(float[] irisRadius, int faceIndex);

    /** This function returns the action unit count.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getActionUnitCount();


    /** This function returns the name of the action unit with the specified index.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern string _getActionUnitName(int auIndex);


    /** This function returns true if the action unit is used.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern bool _getActionUnitUsed(int auIndex);


    /** This function returns all action unit values.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _getActionUnitValues(float[] values, int faceIndex);

    /** This function sets tracking configuration. 
   * 
   * Implemented in VisageTrackerUnityPlugin library.
   */
    [DllImport(dllName)]
    public static extern void _setTrackerConfiguration(string trackerConfigFile, bool au_fitting_disabled, bool mesh_fitting_disabled);

    /** This function sets the inter pupillary distance. 
   * 
   * Implemented in VisageTrackerUnityPlugin library.
   */
    [DllImport(dllName)]
    public static extern void _setIPD(float IPDvalue);

    /** This function returns the current inter pupillary distance (IPD) setting. 
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern float _getIPD();

    #endregion


    #region Analyser

    //
    /** This function initialises the analyser.
   * 
   * Implemented in VisageTrackerUnityPlugin library.
   */
    [DllImport(dllName)]
    public static extern int _initAnalyser(string dataPath);


    /** This function releases memory allocated by the face analyser in the _initAnalyser() function.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _releaseAnalyser();
    
	/** This function performs face analysis on a single image.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
	[DllImport(dllName)]
    public static extern VFAReturnCode _analyseImage(int faceIndex, int options, ref ManagedAnalysisData results);

	/** This function performs face analysis on a series of consecutive frames.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern VFAReturnCode _analyseStream(int faceIndex, int options, ref ManagedAnalysisData results);
    
    /** This function resets the face analysis data for all faces, or for a particular face.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _resetStreamAnalysis(int faceIndex);
    
    /** This function prepares data obtained by the track function.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern bool _prepareDataForAnalysis();

    #endregion


    #region Recognition 

    /** This function initialises the recognition.
     * 
     * Implemented in VisageTrackerUnityPlugin library.
     */
    [DllImport(dllName)]
    public static extern bool _initRecognition(string dataPath);


    /** This function releases the recognition.
 * 
 * Implemented in VisageTrackerUnityPlugin library.
 */
    [DllImport(dllName)]
    public static extern void _releaseRecognition();


    /** This function returns size of descriptor.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getDescriptorSize();


    /** This function extracts descriptor from image.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _extractDescriptor(short[] descriptor, int faceIndex);


    /** This function compares two descriptors.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern float _descriptorsSimilarity(short[] firstDescrtiptor, short[] secondDescriptor);


    /** This function adds descriptor to the gallery.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _addDescriptor(short[] descriptor, string name);


    /** This function returns number of descriptors in the gallery.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _getDescriptorCount();


    /** This function returns the name of a descriptor at the given index in the gallery.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName, CharSet = CharSet.Ansi)]
    public static extern string _getDescriptorName(int galleryIndex);


    /** This function replaces the name of a descriptor at the given index in the gallery with new name. 
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _replaceDescriptorName(string name, int galleryIndex);


    /** This function removes a descriptor at the given index from the gallery.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _removeDescriptor(int galleryIndex);


    /** This function saves gallery in a binary file. 
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _saveGallery(string file_name);


    /** This function loads gallery from a binary file.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern int _loadGallery(string file_name);


    /** This function reset gallery from a binary file.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern void _resetGallery();


    /** This function extracts descriptor from image.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName)]
    public static extern bool _prepareDataForRecog();


    /** This function compares a face to all faces in the current gallery and return n names of the most similar faces.
    * 
    * Implemented in VisageTrackerUnityPlugin library.
    */
    [DllImport(dllName, CharSet = CharSet.Ansi)]
    static extern int _recognize(short[] descriptor, int n, System.Text.StringBuilder[] names, int names_cap, float[] similarities);

    public static int _recognizeWrapper(short[] descriptor, int n, List<string> names, float[] similarities)
    {
        const int SB_CAPACITY = 50;
        //prepare stringbuilder

        System.Text.StringBuilder[] sbArray = new System.Text.StringBuilder[n];
        for (int i = 0; i < n; ++i)
        {
            sbArray[i] = new System.Text.StringBuilder(SB_CAPACITY);
        }
        //
        int ret = _recognize(descriptor, n, sbArray, SB_CAPACITY, similarities);
        //fill list of strings
        for (int i = 0; i < n; ++i)
        {
            names.Add(sbArray[i].ToString());
        }

        return ret;
    }

    #endregion
#endif

}
