using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Target Settings")]
    [SerializeField] private RodController rodController;
    [SerializeField] private float followSpeed = 2f;

    [Header("Zoom Settings")]
    [SerializeField] private float minOrthoSize = 5f;  // Minimum camera size (when hook is near surface)
    [SerializeField] private float maxOrthoSize = 15f; // Maximum camera size (when hook is deep)
    [SerializeField] private float zoomSpeed = 2f;     // How fast the camera zooms
    [SerializeField] private float depthZoomStart = 5f; // Depth at which camera starts zooming out

    [Header("Darkness Settings")]
    [SerializeField] private bool enableDarknessOverlay = true; // Toggle for darkness system
    [SerializeField] private float transitionSpeed = 8f; // Even faster transitions
    [SerializeField] private Color darknessColor = Color.black; // Pure black for complete darkness
    [SerializeField] [Range(0, 1)] private float middleZoneDarkness = 0.85f; // Almost complete darkness in middle
    [SerializeField] [Range(0, 1)] private float deepZoneDarkness = 1.0f; // Complete darkness

    [Header("Level References")]
    [SerializeField] private LevelZone shallowZone;
    [SerializeField] private LevelZone middleZone;
    [SerializeField] private LevelZone deepZone;

    [Header("Camera Bounds")]
    [SerializeField] private Transform boundsReference; // Reference transform to define bounds
    [SerializeField] private Vector2 boundsSize = new Vector2(20f, 20f); // Size of the bounds
    [SerializeField] private float boundsMargin = 1f; // Margin around the bounds

    private Camera mainCamera;
    private Transform hookTransform;
    private Vector3 velocity = Vector3.zero;
    private float currentOrthoSize;
    private float minX, maxX, minY, maxY;

    // Darkness overlay
    private UnityEngine.UI.Image darknessOverlay;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (!mainCamera) mainCamera = Camera.main;
        
        if (!rodController)
        {
            rodController = FindObjectOfType<RodController>();
            Debug.Log($"Found Rod Controller: {rodController != null}");
        }

        // Find level zones if not assigned
        if (!shallowZone || !middleZone || !deepZone)
        {
            LevelZone[] zones = FindObjectsOfType<LevelZone>();
            Debug.Log($"Found {zones.Length} Level Zones");
            foreach (LevelZone zone in zones)
            {
                switch (zone.level)
                {
                    case WaterLevel.Shallow:
                        shallowZone = zone;
                        Debug.Log("Found Shallow Zone");
                        break;
                    case WaterLevel.Middle:
                        middleZone = zone;
                        Debug.Log("Found Middle Zone");
                        break;
                    case WaterLevel.Deep:
                        deepZone = zone;
                        Debug.Log("Found Deep Zone");
                        break;
                }
            }
        }

        // Set up camera bounds
        if (boundsReference != null)
        {
            // Use bounds reference position and size
            Vector2 center = boundsReference.position;
            minX = center.x - boundsSize.x * 0.5f + boundsMargin;
            maxX = center.x + boundsSize.x * 0.5f - boundsMargin;
            minY = center.y - boundsSize.y * 0.5f + boundsMargin;
            maxY = center.y + boundsSize.y * 0.5f - boundsMargin;
        }
        else if (shallowZone && deepZone)
        {
            // Fallback to level zone bounds
            BoxCollider2D shallowCollider = shallowZone.GetComponent<BoxCollider2D>();
            BoxCollider2D deepCollider = deepZone.GetComponent<BoxCollider2D>();

            if (shallowCollider && deepCollider)
            {
                minX = Mathf.Min(shallowCollider.bounds.min.x, deepCollider.bounds.min.x);
                maxX = Mathf.Max(shallowCollider.bounds.max.x, deepCollider.bounds.max.x);
                maxY = shallowCollider.bounds.max.y;
                minY = deepCollider.bounds.min.y;
                depthZoomStart = Mathf.Abs(shallowCollider.bounds.max.y - shallowCollider.bounds.min.y);
            }
        }

        currentOrthoSize = minOrthoSize;
        mainCamera.orthographicSize = currentOrthoSize;

        // Create darkness overlay only if enabled
        if (enableDarknessOverlay)
        {
            CreateDarknessOverlay();
        }
    }

    private void UpdateCameraPosition()
    {
        if (!hookTransform) return;

        // Calculate target position
        Vector3 targetPos = hookTransform.position;
        targetPos.z = transform.position.z;

        // Calculate camera viewport size
        float orthographicWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float orthographicHeight = mainCamera.orthographicSize;

        // Clamp target position to keep camera within bounds
        targetPos.x = Mathf.Clamp(targetPos.x, minX + orthographicWidth, maxX - orthographicWidth);
        targetPos.y = Mathf.Clamp(targetPos.y, minY + orthographicHeight, maxY - orthographicHeight);

        // Smoothly move camera
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            1f / followSpeed
        );
    }

    private void UpdateCameraZoom()
    {
        // Calculate desired zoom based on hook depth
        float hookDepth = Mathf.Abs(hookTransform.position.y);
        float depthRatio = Mathf.Clamp01((hookDepth - depthZoomStart) / (maxY - depthZoomStart));
        float targetOrthoSize = Mathf.Lerp(minOrthoSize, maxOrthoSize, depthRatio);

        // Smoothly adjust camera size
        currentOrthoSize = Mathf.Lerp(
            currentOrthoSize,
            targetOrthoSize,
            Time.deltaTime * zoomSpeed
        );
        
        mainCamera.orthographicSize = currentOrthoSize;
    }

    private void UpdateDarknessOverlay()
    {
        if (darknessOverlay == null)
        {
            Debug.LogWarning("Darkness overlay is missing!");
            return;
        }

        Vector2 hookPosition = hookTransform.position;
        float targetDarkness = 0f;

        // Check which zone the hook is in and calculate darkness
        bool inAnyZone = false;
        
        if (deepZone && deepZone.IsInZone(hookPosition))
        {
            inAnyZone = true;
            float zoneProgress = GetDepthProgressInZone(hookPosition, deepZone);
            // More aggressive darkness in deep zone - using exponential curve
            targetDarkness = Mathf.Lerp(middleZoneDarkness, deepZoneDarkness, Mathf.Pow(zoneProgress, 0.5f));
            Debug.Log($"In Deep Zone - Progress: {zoneProgress:F2}, Target Darkness: {targetDarkness:F2}");
        }
        else if (middleZone && middleZone.IsInZone(hookPosition))
        {
            inAnyZone = true;
            float zoneProgress = GetDepthProgressInZone(hookPosition, middleZone);
            // Much more aggressive darkness in middle
            targetDarkness = Mathf.Lerp(0.4f, middleZoneDarkness, Mathf.Pow(zoneProgress, 0.6f));
            Debug.Log($"In Middle Zone - Progress: {zoneProgress:F2}, Target Darkness: {targetDarkness:F2}");
        }
        else if (shallowZone && shallowZone.IsInZone(hookPosition))
        {
            inAnyZone = true;
            // More noticeable darkness in shallow water
            targetDarkness = 0.3f;
            Debug.Log("In Shallow Zone - Light darkness");
        }

        if (!inAnyZone)
        {
            Debug.Log("Hook is not in any zone!");
            targetDarkness = 0f;
        }

        // Faster darkness transitions
        Color currentColor = darknessOverlay.color;
        float newAlpha = Mathf.MoveTowards(currentColor.a, targetDarkness, Time.deltaTime * transitionSpeed * 2f);
        currentColor = Color.black;
        currentColor.a = newAlpha;
        darknessOverlay.color = currentColor;
        
        Debug.Log($"Darkness Update - Target: {targetDarkness:F2}, Current: {newAlpha:F2}");
    }

    private float GetDepthProgressInZone(Vector2 position, LevelZone zone)
    {
        BoxCollider2D collider = zone.GetComponent<BoxCollider2D>();
        if (collider == null) return 0f;

        // Calculate progress based on vertical position within the zone
        float zoneHeight = collider.bounds.size.y;
        float relativeDepth = collider.bounds.max.y - position.y;
        return Mathf.Clamp01(relativeDepth / zoneHeight);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw camera bounds
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 1f);
        Gizmos.DrawWireCube(center, size);

        // Draw camera viewport bounds
        if (mainCamera != null)
        {
            Gizmos.color = Color.cyan;
            float orthographicWidth = mainCamera.orthographicSize * mainCamera.aspect;
            float orthographicHeight = mainCamera.orthographicSize;
            Vector3 viewportSize = new Vector3(orthographicWidth * 2, orthographicHeight * 2, 1f);
            Gizmos.DrawWireCube(transform.position, viewportSize);
        }

        // Draw zoom start line
        if (shallowZone)
        {
            Gizmos.color = Color.cyan;
            float zoomLineY = shallowZone.transform.position.y - depthZoomStart;
            Vector3 lineStart = new Vector3(minX, zoomLineY, 0);
            Vector3 lineEnd = new Vector3(maxX, zoomLineY, 0);
            Gizmos.DrawLine(lineStart, lineEnd);
        }
    }

    // Add method to toggle darkness overlay at runtime
    public void SetDarknessOverlayEnabled(bool enabled)
    {
        enableDarknessOverlay = enabled;
        if (darknessOverlay != null)
        {
            darknessOverlay.gameObject.transform.parent.gameObject.SetActive(enabled);
        }
        else if (enabled)
        {
            CreateDarknessOverlay();
        }
    }

    private void OnValidate()
    {
        // Update darkness overlay when toggled in inspector during play mode
        if (Application.isPlaying)
        {
            SetDarknessOverlayEnabled(enableDarknessOverlay);
        }

        // Update zone gizmos visibility
        if (shallowZone) shallowZone.showZone = showDebugGizmos;
        if (middleZone) middleZone.showZone = showDebugGizmos;
        if (deepZone) deepZone.showZone = showDebugGizmos;
    }

    private void CreateDarknessOverlay()
    {
        // Create a new Canvas for the overlay
        GameObject overlayCanvas = new GameObject("DarknessOverlay");
        overlayCanvas.transform.SetParent(transform); // Parent to camera to keep it organized
        Canvas canvas = overlayCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCamera;
        canvas.sortingOrder = 999; // Ensure it renders on top of everything
        canvas.planeDistance = 1; // Render very close to camera
        
        // Add a Canvas Scaler for proper scaling
        CanvasScaler scaler = overlayCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Create the darkness overlay image
        GameObject overlayObj = new GameObject("Darkness");
        overlayObj.transform.SetParent(overlayCanvas.transform, false);
        darknessOverlay = overlayObj.AddComponent<UnityEngine.UI.Image>();
        
        // Make it completely solid black
        darknessOverlay.color = new Color(0, 0, 0, 0);
        darknessOverlay.raycastTarget = false; // Prevent it from blocking input

        // Add a second layer of darkness for complete opacity
        GameObject overlayObj2 = new GameObject("Darkness2");
        overlayObj2.transform.SetParent(overlayCanvas.transform, false);
        Image darknessOverlay2 = overlayObj2.AddComponent<UnityEngine.UI.Image>();
        darknessOverlay2.color = new Color(0, 0, 0, 0);
        darknessOverlay2.raycastTarget = false;
        
        // Set both overlays to cover the entire screen
        RectTransform rect = darknessOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        RectTransform rect2 = darknessOverlay2.rectTransform;
        rect2.anchorMin = Vector2.zero;
        rect2.anchorMax = Vector2.one;
        rect2.sizeDelta = Vector2.zero;
        rect2.anchoredPosition = Vector2.zero;

        Debug.Log("Double-layer darkness overlay created successfully!");
    }

    private void LateUpdate()
    {
        if (!rodController)
        {
            Debug.LogWarning("Rod Controller is missing!");
            return;
        }

        if (!rodController.hook)
        {
            Debug.LogWarning("Current Hook is null!");
            return;
        }

        hookTransform = rodController.hook.transform;
        UpdateCameraPosition();
        UpdateCameraZoom();
        
        // Only update darkness if enabled
        if (enableDarknessOverlay && darknessOverlay != null)
        {
            UpdateDarknessOverlay();
        }
    }
} 