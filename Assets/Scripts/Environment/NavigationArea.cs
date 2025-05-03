using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[RequireComponent(typeof(BoxCollider2D))]
public class NavigationArea : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);

    private BoxCollider2D boxCollider;
    private NavMeshModifier navModifier;

    private void Awake()
    {
        // Get or add required components
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true; // Make it a trigger so it doesn't affect physics

        // Add NavMeshModifier
        navModifier = GetComponent<NavMeshModifier>();
        if (navModifier == null)
        {
            navModifier = gameObject.AddComponent<NavMeshModifier>();
        }
        navModifier.overrideArea = true;
        navModifier.area = 0; // 0 is "Walkable" in NavMesh areas
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

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
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
        }

        // Restore the previous gizmo color
        Gizmos.color = previousColor;
    }

    // Helper method to set the size of the navigation area
    public void SetSize(Vector2 size)
    {
        if (boxCollider != null)
        {
            boxCollider.size = size;
        }
    }

    // Helper method to set the offset of the navigation area
    public void SetOffset(Vector2 offset)
    {
        if (boxCollider != null)
        {
            boxCollider.offset = offset;
        }
    }
} 