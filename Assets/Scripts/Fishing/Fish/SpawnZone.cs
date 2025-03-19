using UnityEngine;

public class SpawnZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public string zoneName = "Shallow Waters";
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.2f);
    
    private BoxCollider2D zoneCollider;
    
    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        if (zoneCollider == null)
        {
            zoneCollider = gameObject.AddComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!enabled) return;
        
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Draw filled area
            Gizmos.color = gizmoColor;
            Vector3 center = transform.TransformPoint(boxCollider.offset);
            Vector3 size = new Vector3(
                boxCollider.size.x * transform.lossyScale.x,
                boxCollider.size.y * transform.lossyScale.y,
                1f
            );
            Gizmos.DrawCube(center, size);
            
            // Draw wireframe
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
} 