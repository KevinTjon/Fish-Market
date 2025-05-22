using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus.Components;

namespace Market
{
    public class NavMeshManager : MonoBehaviour
    {
        private NavMeshSurface navMeshSurface;
        private bool isNavMeshReady = false;

        public static NavMeshManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            navMeshSurface = GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
                ConfigureNavMeshSurface();
            }
        }

        private void ConfigureNavMeshSurface()
        {
            if (navMeshSurface == null) return;

            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.defaultArea = 0; // Walkable
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.layerMask = -1; // All layers
            navMeshSurface.ignoreNavMeshAgent = true;
            navMeshSurface.ignoreNavMeshObstacle = false;
            navMeshSurface.overrideTileSize = true;
            navMeshSurface.tileSize = 256; // Adjust based on your map size
            navMeshSurface.minRegionArea = 4; // Minimum area size to consider
            navMeshSurface.overrideVoxelSize = true;
            navMeshSurface.voxelSize = 0.1f; // Adjust based on your tilemap grid size
        }

        public void BakeNavMesh()
        {
            if (navMeshSurface == null)
            {
                Debug.LogError("NavMeshSurface not found!");
                return;
            }

            navMeshSurface.BuildNavMesh();
            isNavMeshReady = true;
            Debug.Log("NavMesh baked successfully!");
        }

        public bool IsNavMeshReady()
        {
            if (!isNavMeshReady) return false;
            
            // Additional check to verify NavMesh exists
            NavMeshHit hit;
            Vector3 randomPoint = transform.position;
            return NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas);
        }

        private void Start()
        {
            // Automatically bake on start
            BakeNavMesh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface != null)
                {
                    ConfigureNavMeshSurface();
                }
            }
        }
#endif
    }
} 