using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public HookController target;
    public float followSpeed = 5f;
    
    [Header("Zoom Settings")]
    public float orthoSize = 3f;    // Fixed camera size
    
    [Header("Bounds Settings")]
    public BoxCollider2D cameraBounds;
    public Vector2 padding = new Vector2(2f, 2f);
    
    private Camera mainCamera;
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private Vector2 screenHalfSize;
    
    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No camera found!");
                enabled = false;
                return;
            }
        }
        
        // Find target if not assigned
        if (target == null)
        {
            target = FindObjectOfType<HookController>();
            if (target == null)
            {
                var rodController = FindObjectOfType<RodController>();
                if (rodController != null && rodController.hook != null)
                {
                    target = rodController.hook;
                    Debug.Log("Found hook through RodController");
                }
            }
        }
        
        // Set fixed camera size
        mainCamera.orthographicSize = orthoSize;
        
        // Calculate screen half size
        screenHalfSize.y = mainCamera.orthographicSize;
        screenHalfSize.x = screenHalfSize.y * mainCamera.aspect;
        
        // Find camera bounds if not assigned
        if (cameraBounds == null)
        {
            cameraBounds = FindObjectOfType<BoxCollider2D>();
            if (cameraBounds != null && cameraBounds.gameObject.name.Contains("Boundary"))
            {
                Debug.Log("Found boundary collider for camera bounds");
            }
        }
        
        // Initial position
        if (target != null)
        {
            UpdateCameraPosition();
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        UpdateCameraPosition();
    }
    
    private void UpdateCameraPosition()
    {
        // Set target position to hook position
        targetPosition = target.transform.position;
        targetPosition.z = transform.position.z;
        
        // Apply bounds if we have them
        if (cameraBounds != null)
        {
            Bounds bounds = cameraBounds.bounds;
            
            // Calculate camera bounds with padding
            float minX = bounds.min.x + screenHalfSize.x + padding.x;
            float maxX = bounds.max.x - screenHalfSize.x - padding.x;
            float minY = bounds.min.y + screenHalfSize.y + padding.y;
            float maxY = bounds.max.y - screenHalfSize.y - padding.y;
            
            // Clamp target position
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }
        
        // Smoothly move camera
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, 
            ref velocity, 0.2f, followSpeed);
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || target == null) return;
        
        // Draw focus point
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        
        // Draw camera bounds if available
        if (cameraBounds != null)
        {
            // Draw camera bounds
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Bounds bounds = cameraBounds.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            
            // Draw effective camera bounds (with padding)
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Vector3 paddedSize = new Vector3(
                bounds.size.x - (padding.x * 2 + screenHalfSize.x * 2),
                bounds.size.y - (padding.y * 2 + screenHalfSize.y * 2),
                bounds.size.z);
            Gizmos.DrawWireCube(bounds.center, paddedSize);
        }
    }
} 