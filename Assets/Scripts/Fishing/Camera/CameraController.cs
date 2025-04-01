using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private SimpleRodController rodController;
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private float verticalOffset = 2f; // Camera will stay this far above the hook

    [Header("Zoom Settings")]
    [SerializeField] private float minOrthoSize = 5f;  // Minimum camera size (when hook is near surface)
    [SerializeField] private float maxOrthoSize = 15f; // Maximum camera size (when hook is deep)
    [SerializeField] private float zoomSpeed = 2f;     // How fast the camera zooms
    [SerializeField] private float depthZoomStart = 5f; // Depth at which camera starts zooming out
    
    [Header("Bounds Settings")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;
    [SerializeField] private float minY = -30f;
    [SerializeField] private float maxY = 5f;

    private Camera mainCamera;
    private Transform hookTransform;
    private Vector3 velocity = Vector3.zero;
    private float currentOrthoSize;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (!mainCamera) mainCamera = Camera.main;
        
        if (!rodController)
            rodController = FindObjectOfType<SimpleRodController>();

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
        // Calculate target position
        Vector3 targetPos = hookTransform.position;
        
        // Add vertical offset
        targetPos.y += verticalOffset;
        
        // Keep camera's z position
        targetPos.z = transform.position.z;
        
        // Clamp position within bounds
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

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
    }
} 