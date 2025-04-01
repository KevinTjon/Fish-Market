using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private SimpleRodController rodController;
    [SerializeField] private float followSpeed = 2f;

    [Header("Zoom Settings")]
    [SerializeField] private float minOrthoSize = 5f;  // Minimum camera size (when hook is near surface)
    [SerializeField] private float maxOrthoSize = 15f; // Maximum camera size (when hook is deep)
    [SerializeField] private float zoomSpeed = 2f;     // How fast the camera zooms
    [SerializeField] private float depthZoomStart = 5f; // Depth at which camera starts zooming out

    [Header("Level References")]
    [SerializeField] private LevelZone shallowZone;
    [SerializeField] private LevelZone deepZone;

    private Camera mainCamera;
    private Transform hookTransform;
    private Vector3 velocity = Vector3.zero;
    private float currentOrthoSize;
    private float minX, maxX, minY, maxY;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (!mainCamera) mainCamera = Camera.main;
        
        if (!rodController)
            rodController = FindObjectOfType<SimpleRodController>();

        // Find level zones if not assigned
        if (!shallowZone || !deepZone)
        {
            LevelZone[] zones = FindObjectsOfType<LevelZone>();
            foreach (LevelZone zone in zones)
            {
                if (zone.level == WaterLevel.Shallow)
                    shallowZone = zone;
                else if (zone.level == WaterLevel.Deep)
                    deepZone = zone;
            }
        }

        // Set camera bounds based on level zones
        if (shallowZone && deepZone)
        {
            BoxCollider2D shallowCollider = shallowZone.GetComponent<BoxCollider2D>();
            BoxCollider2D deepCollider = deepZone.GetComponent<BoxCollider2D>();

            if (shallowCollider && deepCollider)
            {
                // Get the leftmost and rightmost points from both colliders
                minX = Mathf.Min(shallowCollider.bounds.min.x, deepCollider.bounds.min.x);
                maxX = Mathf.Max(shallowCollider.bounds.max.x, deepCollider.bounds.max.x);
                
                // Top of shallow zone to bottom of deep zone
                maxY = shallowCollider.bounds.max.y;
                minY = deepCollider.bounds.min.y;

                // Adjust depthZoomStart based on zones
                depthZoomStart = Mathf.Abs(shallowCollider.bounds.max.y - shallowCollider.bounds.min.y);
            }
        }

        currentOrthoSize = minOrthoSize;
        mainCamera.orthographicSize = currentOrthoSize;
    }

    private void LateUpdate()
    {
        if (!rodController || !rodController.CurrentHook) return;

        hookTransform = rodController.CurrentHook.transform;
        UpdateCameraPosition();
        UpdateCameraZoom();
    }

    private void UpdateCameraPosition()
    {
        // Simply center on hook
        Vector3 targetPos = hookTransform.position;
        targetPos.z = transform.position.z;

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

    private void OnDrawGizmos()
    {
        // Draw camera bounds
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 1f);
        Gizmos.DrawWireCube(center, size);

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
} 