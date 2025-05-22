using UnityEngine;

public class MarketplaceCameraController : MonoBehaviour
{
    private Transform target;
    [SerializeField] private BoxCollider2D cameraBounds;
    [SerializeField] private float smoothSpeed = 2.5f;
    [SerializeField] private Vector2 followOffset = Vector2.zero;
    [SerializeField] private bool debugMode = true;
    
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;
    
    private float halfHeight;
    private float halfWidth;
    private Vector3 velocity;
    private bool boundsInitialized = false;
    private Vector3 lastTargetPosition;

    private void OnEnable()
    {
        if (debugMode)
        {
            Debug.Log("MarketplaceCameraController enabled - Debug mode is ON");
        }
    }
    
    private void Start()
    {
        if (debugMode)
        {
            Debug.Log("MarketplaceCameraController starting initialization...");
        }
        InitializeBounds();
    }

    private void InitializeBounds()
    {
        if (cameraBounds == null)
        {
            Debug.LogError("ERROR: Camera bounds not assigned! Please assign a BoxCollider2D to the camera bounds field.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("ERROR: No main camera found! Make sure your camera is tagged as 'MainCamera'.");
            return;
        }

        // Calculate camera viewport size in world units
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
        
        Debug.Log($"Camera setup - Orthographic Size: {halfHeight}, Aspect: {cam.aspect}");
        Debug.Log($"Camera viewport size - Width: {halfWidth * 2}, Height: {halfHeight * 2}");

        // Get the bounds from the collider
        Debug.Log($"Camera bounds - Center: {cameraBounds.bounds.center}, Size: {cameraBounds.bounds.size}");
        Bounds bounds = cameraBounds.bounds;
        Vector2 boundsMin = bounds.min;
        Vector2 boundsMax = bounds.max;

        // Ensure min is actually less than max
        minX = Mathf.Min(boundsMin.x, boundsMax.x) + halfWidth;
        maxX = Mathf.Max(boundsMin.x, boundsMax.x) - halfWidth;
        minY = Mathf.Min(boundsMin.y, boundsMax.y) + halfHeight;
        maxY = Mathf.Max(boundsMin.y, boundsMax.y) - halfHeight;

        Debug.Log($"Camera bounds - Min: ({minX}, {minY}), Max: ({maxX}, {maxY})");
        // Validate the bounds
        if (maxX <= minX || maxY <= minY)
        {
            Debug.LogWarning($"Camera bounds are too small for the camera view! Need at least {halfWidth * 2} width and {halfHeight * 2} height.");
            // Set some minimal bounds to prevent errors
            float centerX = (boundsMin.x + boundsMax.x) * 0.5f;
            float centerY = (boundsMin.y + boundsMax.y) * 0.5f;
            minX = centerX - halfWidth;
            maxX = centerX + halfWidth;
            minY = centerY - halfHeight;
            maxY = centerY + halfHeight;
        }
        
        Debug.Log($"Raw bounds - Min: ({boundsMin.x}, {boundsMin.y}), Max: ({boundsMax.x}, {boundsMax.y})");
        Debug.Log($"Camera movement limits - X: {minX:F2} to {maxX:F2}, Y: {minY:F2} to {maxY:F2}");
        
        boundsInitialized = true;

        // Try to find player immediately
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            Debug.Log($"Found player! Name: {player.name}, Position: {player.transform.position}, Active: {player.activeInHierarchy}");

            // Set initial camera position
            Vector3 targetPos = GetTargetPosition();
            transform.position = targetPos;
            lastTargetPosition = targetPos;
            velocity = Vector3.zero;
            //Debug.Log($"Set initial camera position to: {targetPos}");
        }
        else
        {
            Debug.LogWarning("No player found with 'Player' tag! Make sure your player object has the correct tag.");
        }
    }

    private void Update()
    {
        if (target == null)
        {
            FindPlayer();
        }
    }

    private Vector3 GetTargetPosition()
    {
        if (target == null) return transform.position;

        Vector3 targetPos = target.position;
        targetPos.x += followOffset.x;
        targetPos.y += followOffset.y;
        targetPos.z = transform.position.z;

        // Clamp to bounds
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        return targetPos;
    }

    private void LateUpdate()
    {
        if (target == null || cameraBounds == null || !boundsInitialized) 
        {
            if (debugMode)
            {
                Debug.LogWarning($"Camera update skipped - Target: {(target != null ? "Found" : "Missing")}, " +
                               $"Bounds: {(cameraBounds != null ? "Found" : "Missing")}, " +
                               $"Initialized: {boundsInitialized}");
            }
            return;
        }

        Vector3 targetPosition = GetTargetPosition();

        // Check if target has moved significantly
        if (Vector3.Distance(targetPosition, lastTargetPosition) > 10f)
        {
            // If large jump, snap to new position
            transform.position = targetPosition;
            velocity = Vector3.zero;
            if (debugMode) Debug.Log("Large position change detected - snapping camera to target");
        }
        else
        {
            // Normal smooth movement
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                1f / smoothSpeed,
                Mathf.Infinity,
                Time.deltaTime
            );

            transform.position = newPosition;
        }

        lastTargetPosition = targetPosition;

        if (debugMode)
        {
           // Debug.Log($"Camera Update - Position: {transform.position}, Target: {targetPosition}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        if (cameraBounds != null)
        {
            // Draw the camera bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cameraBounds.bounds.center, cameraBounds.bounds.size);

            // Draw the actual camera movement bounds
            if (boundsInitialized)
            {
                Gizmos.color = Color.green;
                Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
                Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
                Gizmos.DrawWireCube(center, size);

                // Draw lines to target if it exists
                if (target != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.position, target.position);
                    Gizmos.DrawWireSphere(target.position, 0.5f);
                }
            }
        }
    }
} 