using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles collision detection and interaction with tilemaps.
/// Attach this script to any GameObject with a Tilemap component.
/// </summary>
[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapCollider2D))]
public class TilemapCollisionHandler : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers = Physics2D.AllLayers; // Which layers to check collisions against
    [SerializeField] private bool showDebugCollisions = false;

    private Tilemap tilemap;
    private TilemapCollider2D tilemapCollider;
    private ContactFilter2D contactFilter;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        
        if (tilemap == null || tilemapCollider == null)
        {
            Debug.LogError("TilemapCollisionHandler requires both Tilemap and TilemapCollider2D components!");
            enabled = false;
            return;
        }

        // Set up contact filter with specified layers
        contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(collisionLayers);
        contactFilter.useLayerMask = true;

        if (showDebugCollisions)
        {
            Debug.Log($"TilemapCollisionHandler initialized on {gameObject.name} checking against layers: {collisionLayers.value}");
        }
    }

    /// <summary>
    /// Checks if a position is colliding with the tilemap on the specified layers
    /// </summary>
    /// <param name="position">World position to check</param>
    /// <returns>True if there is a collidable tile at the position</returns>
    public bool IsColliding(Vector2 position)
    {
        // Create a small circle at the position to check for collisions
        Collider2D[] results = new Collider2D[1];
        int numColliders = Physics2D.OverlapCircle(position, 0.1f, contactFilter, results);
        
        if (showDebugCollisions && numColliders > 0)
        {
            Debug.Log($"Collision detected at {position} with {results[0].gameObject.name} on layer {results[0].gameObject.layer}");
        }

        return numColliders > 0;
    }

    /// <summary>
    /// Gets the tile at a specific world position
    /// </summary>
    /// <param name="position">World position to check</param>
    /// <returns>The tile at the position, or null if no tile exists</returns>
    public TileBase GetTileAtPosition(Vector2 position)
    {
        Vector3Int tilePosition = tilemap.WorldToCell(position);
        return tilemap.GetTile(tilePosition);
    }

    /// <summary>
    /// Gets the tile position in grid coordinates for a world position
    /// </summary>
    /// <param name="worldPosition">World position to convert</param>
    /// <returns>Grid position of the tile</returns>
    public Vector3Int GetTilePosition(Vector2 worldPosition)
    {
        return tilemap.WorldToCell(worldPosition);
    }

    /// <summary>
    /// Gets the world position of a tile's center
    /// </summary>
    /// <param name="tilePosition">Grid position of the tile</param>
    /// <returns>World position of the tile's center</returns>
    public Vector2 GetWorldPosition(Vector3Int tilePosition)
    {
        return tilemap.GetCellCenterWorld(tilePosition);
    }

    /// <summary>
    /// Checks if a position is within the bounds of the tilemap
    /// </summary>
    /// <param name="position">World position to check</param>
    /// <returns>True if the position is within the tilemap bounds</returns>
    public bool IsWithinBounds(Vector2 position)
    {
        BoundsInt bounds = tilemap.cellBounds;
        Vector3Int tilePos = tilemap.WorldToCell(position);
        return bounds.Contains(tilePos);
    }

    /// <summary>
    /// Updates which layers to check collisions against
    /// </summary>
    /// <param name="layers">The LayerMask containing the layers to check</param>
    public void SetCollisionLayers(LayerMask layers)
    {
        collisionLayers = layers;
        contactFilter.SetLayerMask(collisionLayers);
    }
} 