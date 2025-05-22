using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using NavMeshPlus.Components;

namespace Market
{
    [RequireComponent(typeof(Tilemap))]
    public class TilemapNavMeshSetup : MonoBehaviour
    {
        public enum TilemapType
        {
            Ground,
            Wall
        }

        [Header("NavMesh Settings")]
        [SerializeField] private TilemapType tilemapType = TilemapType.Ground;
        [SerializeField] private bool generateColliders = true;
        [SerializeField] private float colliderDepth = 2f;

        private Tilemap tilemap;
        private TilemapCollider2D tilemapCollider;
        private CompositeCollider2D compositeCollider;
        private Rigidbody2D rb2d;

        private void Awake()
        {
            tilemap = GetComponent<Tilemap>();

            if (generateColliders && tilemapType == TilemapType.Wall)
            {
                SetupColliders();
            }
        }

        private void SetupColliders()
        {
            // Add TilemapCollider2D if it doesn't exist
            tilemapCollider = GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
            {
                tilemapCollider = gameObject.AddComponent<TilemapCollider2D>();
            }
            tilemapCollider.usedByComposite = true;

            // Add Rigidbody2D if it doesn't exist
            rb2d = GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                rb2d = gameObject.AddComponent<Rigidbody2D>();
            }
            rb2d.bodyType = RigidbodyType2D.Static;
            rb2d.simulated = true;
            rb2d.useFullKinematicContacts = false;
            rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Add CompositeCollider2D if it doesn't exist
            compositeCollider = GetComponent<CompositeCollider2D>();
            if (compositeCollider == null)
            {
                compositeCollider = gameObject.AddComponent<CompositeCollider2D>();
            }
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider.isTrigger = false;
            compositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;

            // Add NavMeshModifier if it doesn't exist
            var navMeshModifier = GetComponent<NavMeshModifier>();
            if (navMeshModifier == null)
            {
                navMeshModifier = gameObject.AddComponent<NavMeshModifier>();
            }

            // Set NavMesh area based on tilemap type
            if (tilemapType == TilemapType.Wall)
            {
                navMeshModifier.overrideArea = true;
                navMeshModifier.area = 1; // Not Walkable
                
                // Add NavMeshObstacle for walls
                var obstacle = GetComponent<NavMeshObstacle>();
                if (obstacle == null)
                {
                    obstacle = gameObject.AddComponent<NavMeshObstacle>();
                }
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = Vector3.zero;
                obstacle.size = new Vector3(tilemap.localBounds.size.x, tilemap.localBounds.size.y, colliderDepth);
            }
            else
            {
                navMeshModifier.overrideArea = true;
                navMeshModifier.area = 0; // Walkable
            }

            // Generate the composite collider
            compositeCollider.GenerateGeometry();
        }

        private void OnValidate()
        {
            if (generateColliders && Application.isPlaying)
            {
                SetupColliders();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!tilemap) return;

            // Draw debug visualization
            Gizmos.color = tilemapType == TilemapType.Wall ? 
                new Color(1f, 0f, 0f, 0.3f) : // Red for walls
                new Color(0f, 1f, 0f, 0.1f);  // Green for ground

            var bounds = tilemap.localBounds;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
    }
} 