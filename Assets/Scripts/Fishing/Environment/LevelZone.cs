using UnityEngine;

public enum WaterLevel
{
    Shallow,
    Middle,
    Deep
}

[RequireComponent(typeof(BoxCollider2D))]
public class LevelZone : MonoBehaviour
{
    public WaterLevel level;
    public Color zoneColor = new Color(0.5f, 0.8f, 1f, 0.2f);
    public bool showZone = true;

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true; // Make sure it's a trigger collider
    }

    private void OnDrawGizmos()
    {
        if (!showZone) return;

        // Set color based on level
        switch (level)
        {
            case WaterLevel.Shallow:
                Gizmos.color = new Color(0.5f, 1f, 1f, 0.2f); // Light blue
                break;
            case WaterLevel.Middle:
                Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.2f); // Medium blue
                break;
            case WaterLevel.Deep:
                Gizmos.color = new Color(0f, 0.4f, 1f, 0.2f); // Dark blue
                break;
        }

        // Draw zone
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Draw filled area
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            
            // Draw outline
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
        }
    }

    // Helper method to check if a position is within this zone
    public bool IsInZone(Vector2 position)
    {
        if (boxCollider == null) return false;
        return boxCollider.bounds.Contains(position);
    }

    // Helper method to get a random position within this zone
    public Vector2 GetRandomPosition()
    {
        if (boxCollider == null) return Vector2.zero;
        
        Bounds bounds = boxCollider.bounds;
        return new Vector2(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y)
        );
    }
} 