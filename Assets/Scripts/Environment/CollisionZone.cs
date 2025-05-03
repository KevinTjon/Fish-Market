using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CollisionZone : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Make sure it's not a trigger
        boxCollider.isTrigger = false;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        // Cache the previous gizmo color
        Color previousColor = Gizmos.color;
        
        // Set the gizmo color and matrix
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Get collider size and offset
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Draw filled area
            Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            
            // Draw outline
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
        }
        else
        {
            // Draw default size if no collider
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        // Restore the previous gizmo color
        Gizmos.color = previousColor;
    }
} 