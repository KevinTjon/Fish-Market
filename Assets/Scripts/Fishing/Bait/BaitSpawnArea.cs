using UnityEngine;

[RequireComponent(typeof(HookController))]
public class BaitSpawnArea : MonoBehaviour
{
    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private float spawnRadius = 0.5f;
    
    // Gizmo visualization
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f); // Green with transparency

    public Vector2 GetRandomSpawnPosition()
    {
        // Get a random point within the spawn radius
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
        
        // Add the offset to position it relative to the hook
        return (Vector2)transform.position + spawnOffset + randomPoint;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw the spawn area
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere((Vector2)transform.position + spawnOffset, spawnRadius);
        
        // Draw a line from the hook to the center of the spawn area
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + spawnOffset);
    }
} 