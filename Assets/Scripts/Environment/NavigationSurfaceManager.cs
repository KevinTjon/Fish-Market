using UnityEngine;
using Unity.AI.Navigation;
using System.Collections.Generic;
using UnityEngine.AI;

// Ensure this script runs before other scripts that need the NavMesh
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(NavMeshSurface))]
public class NavigationSurfaceManager : MonoBehaviour
{
    public static NavigationSurfaceManager Instance { get; private set; }
    public bool IsNavMeshBaked { get; private set; }

    [Header("Collection Settings")]
    [SerializeField] private LayerMask includeLayers = -1; // All layers by default
    [SerializeField] private bool collectObjects = true;
    [SerializeField] private bool includeChildren = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool autoBakeOnStart = true;

    private NavMeshSurface surface;
    private List<NavigationArea> navigationAreas = new List<NavigationArea>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[NavigationSurfaceManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        surface = GetComponent<NavMeshSurface>();
        ConfigureNavMeshSurface();
    }

    private void Start()
    {
        if (autoBakeOnStart)
        {
            RebakeNavMesh();
        }
    }

    private void ConfigureNavMeshSurface()
    {
        if (surface == null) return;

        // Configure the NavMeshSurface
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = includeLayers;
        surface.ignoreNavMeshAgent = false;
        surface.ignoreNavMeshObstacle = false;
        surface.overrideTileSize = false;
        
        if (showDebugLogs)
        {
            Debug.Log("[NavigationSurfaceManager] NavMeshSurface configured:");
            Debug.Log($"- Collection Method: {surface.collectObjects}");
            Debug.Log($"- Use Geometry: {surface.useGeometry}");
            Debug.Log($"- Layer Mask: {surface.layerMask}");
        }
    }

    public void RebakeNavMesh()
    {
        if (surface == null) return;

        if (showDebugLogs)
        {
            Debug.Log("[NavigationSurfaceManager] Starting NavMesh bake...");
        }

        // Find all NavigationArea components
        navigationAreas.Clear();
        navigationAreas.AddRange(FindObjectsOfType<NavigationArea>());

        if (showDebugLogs)
        {
            Debug.Log($"[NavigationSurfaceManager] Found {navigationAreas.Count} NavigationArea components");
            foreach (var area in navigationAreas)
            {
                Debug.Log($"- NavigationArea at {area.transform.position}");
            }
        }

        // Ensure all NavigationArea components are properly set up
        foreach (var area in navigationAreas)
        {
            var modifier = area.GetComponent<NavMeshModifier>();
            if (modifier != null)
            {
                modifier.ignoreFromBuild = false;
                if (showDebugLogs)
                {
                    Debug.Log($"[NavigationSurfaceManager] Configured NavMeshModifier for area at {area.transform.position}");
                }
            }
        }

        try
        {
            // Clear existing NavMesh data
            surface.RemoveData();
            
            // Bake the NavMesh
            surface.BuildNavMesh();
            IsNavMeshBaked = true;

            if (showDebugLogs)
            {
                Debug.Log("[NavigationSurfaceManager] NavMesh bake completed");
                
                // Validate the baked NavMesh
                foreach (var area in navigationAreas)
                {
                    NavMeshHit hit;
                    bool hasNavMesh = NavMesh.SamplePosition(area.transform.position, out hit, 0.1f, NavMesh.AllAreas);
                    Debug.Log($"- Area at {area.transform.position}: {(hasNavMesh ? "Valid" : "Invalid")} NavMesh coverage");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NavigationSurfaceManager] Failed to bake NavMesh: {e.Message}");
            IsNavMeshBaked = false;
        }
    }

    public bool IsPositionOnNavMesh(Vector3 position, float maxDistance = 1.0f)
    {
        if (!IsNavMeshBaked) return false;
        
        NavMeshHit hit;
        return NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas);
    }

    private void OnValidate()
    {
        if (surface == null)
        {
            surface = GetComponent<NavMeshSurface>();
        }
        ConfigureNavMeshSurface();
    }

    // Editor menu item to manually rebake
    [ContextMenu("Rebake NavMesh")]
    private void RebakeNavMeshMenuItem()
    {
        RebakeNavMesh();
    }
} 