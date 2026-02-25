using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tracker))]
public class TrackerCameraOverlay : MonoBehaviour
{
    [Header("Overlay placement")]
    [SerializeField] private Vector2 overlaySize = new Vector2(320f, 180f);
    [SerializeField] private Vector2 screenPadding = new Vector2(20f, 20f);
    [SerializeField] private int canvasSortOrder = 500;

    [Header("Native camera binding")]
    [SerializeField] private bool enableNativeBinding = true;
    [SerializeField] private bool flipVertical = true;

    [Header("Optional explicit target")]
    [SerializeField] private RawImage targetRawImage;

    private Tracker tracker;
    private Texture2D cameraTexture;
    private int sourceImageWidth;
    private int sourceImageHeight;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private Color32[] cameraPixels;
    private GCHandle cameraPixelsHandle;
#endif

    private void Awake()
    {
        tracker = GetComponent<Tracker>();
        EnsureRawImage();
    }

    private void LateUpdate()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!enableNativeBinding || tracker == null || targetRawImage == null || !tracker.frameForAnalysis)
        {
            return;
        }

        VisageTrackerNative._getCameraInfo(out _, out int imageWidth, out int imageHeight);
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return;
        }

        sourceImageWidth = imageWidth;
        sourceImageHeight = imageHeight;

        EnsureTextureAndBuffer(imageWidth, imageHeight);

        if (!cameraPixelsHandle.IsAllocated)
        {
            return;
        }

        VisageTrackerNative._setFrameData(cameraPixelsHandle.AddrOfPinnedObject());
        cameraTexture.SetPixels32(cameraPixels);
        cameraTexture.Apply(false);

        ApplyUvRect();
#else
        // Camera texture binding is currently implemented only for Windows plugin integration.
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void EnsureTextureAndBuffer(int width, int height)
    {
        int texWidth = NextPowerOfTwo(width);
        int texHeight = NextPowerOfTwo(height);

        if (cameraTexture != null && cameraTexture.width == texWidth && cameraTexture.height == texHeight)
        {
            return;
        }

        ReleasePinnedBuffer();

        cameraTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        cameraTexture.wrapMode = TextureWrapMode.Clamp;
        cameraTexture.filterMode = FilterMode.Bilinear;

        cameraPixels = cameraTexture.GetPixels32(0);
        cameraPixelsHandle = GCHandle.Alloc(cameraPixels, GCHandleType.Pinned);

        targetRawImage.texture = cameraTexture;
    }

    private void ApplyUvRect()
    {
        if (cameraTexture == null || sourceImageWidth <= 0 || sourceImageHeight <= 0)
        {
            return;
        }

        float uWidth = Mathf.Clamp01(sourceImageWidth / (float)cameraTexture.width);
        float vHeight = Mathf.Clamp01(sourceImageHeight / (float)cameraTexture.height);

        float x = tracker.isMirrored == 1 ? uWidth : 0f;
        float width = tracker.isMirrored == 1 ? -uWidth : uWidth;

        float y = flipVertical ? vHeight : 0f;
        float height = flipVertical ? -vHeight : vHeight;

        targetRawImage.uvRect = new Rect(x, y, width, height);
    }

    private static int NextPowerOfTwo(int value)
    {
        value = Mathf.Max(1, value);
        return Mathf.NextPowerOfTwo(value);
    }
#endif

    private void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        ReleasePinnedBuffer();
#endif

        if (cameraTexture != null)
        {
            Destroy(cameraTexture);
            cameraTexture = null;
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void ReleasePinnedBuffer()
    {
        if (cameraPixelsHandle.IsAllocated)
        {
            cameraPixelsHandle.Free();
        }

        cameraPixels = null;
    }
#endif

    private void EnsureRawImage()
    {
        if (targetRawImage != null)
        {
            return;
        }

        Canvas overlayCanvas = FindFirstObjectByType<Canvas>();
        if (overlayCanvas == null)
        {
            GameObject canvasObject = new GameObject("CameraOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlayCanvas = canvasObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = canvasSortOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject rawImageObject = new GameObject("CameraOverlay", typeof(RectTransform), typeof(RawImage));
        rawImageObject.transform.SetParent(overlayCanvas.transform, false);

        RectTransform rectTransform = rawImageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.sizeDelta = overlaySize;
        rectTransform.anchoredPosition = new Vector2(screenPadding.x, screenPadding.y);

        targetRawImage = rawImageObject.GetComponent<RawImage>();
        targetRawImage.color = Color.white;
        targetRawImage.raycastTarget = false;
    }
}
