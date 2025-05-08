using UnityEngine;
using FishSizeNamespace;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class HookController : MonoBehaviour
{
    // Position parameters
    public float WaterLevel { get; private set; }
    public bool OnWaterSurface { get; private set; }
    public bool IsFishing { get; private set; }
    
    // Attached object parameters
    public bool HasHookedObject { get; private set; }
    public FishSize AttractedSize { get; private set; }
    
    // Serialized parameters
    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private float spawnRadius = 0.5f;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip hookToWaterSound;
    [SerializeField] private AudioClip fishCaughtSound;

    // Gizmo visualization
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f); // Green with transparency
    [SerializeField] private float hookRadius = 0.2f;
    
    // Unity References
    public GameObject attachedObject { get; private set; }
    public Rigidbody2D hookRB { get; private set; }
    private CircleCollider2D hookCollider;
    private Transform rodConnection;
    private AudioSource source;



    // Add FishSize variable to help with detection
    // Adjust the catching logic to happen here
    private void Awake()
    {
        // Setup collider
        hookCollider = GetComponent<CircleCollider2D>();
        hookCollider.radius = hookRadius;
        hookCollider.isTrigger = true;

        // Set the hook to a specific layer
        gameObject.layer = LayerMask.NameToLayer("Hook");

        hookRB = GetComponent<Rigidbody2D>();
        hookRB.gravityScale = 0; // Disable gravity for the hook if not initialized

        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }
    }

    public void InitializeHook(Transform rodConnection)
    {
        // Setup the hook's Rigidbody2D and Collider2D
        hookRB = GetComponent<Rigidbody2D>();
        hookRB.drag = 0.5f;
        hookRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        hookCollider = GetComponent<CircleCollider2D>();
        this.rodConnection = rodConnection;
        
        // Ensure the Hook layer exists and set up layer collision matrix        
        // This is just a debug check - layer setup should be done in Unity Editor
        if (LayerMask.NameToLayer("Hook") == -1)
        {
            Debug.LogWarning("Hook layer not found");
        }
        
        HasHookedObject = false;
        OnWaterSurface = false;
        IsFishing = false;
    }

    // Only runs if we currently aren't fishing
    private void Update()
    {
        if (!IsFishing && hookRB != null && rodConnection != null)
        {
            hookRB.position = rodConnection.position;
        }
    }

    // Called when we start fishing
    public void StartFishing(Vector2 velocity)
    {
        IsFishing = true;
        hookRB.gravityScale = 1;
        hookRB.velocity = velocity;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the hook has collided with a fish
        BasicFish fish = other.GetComponent<BasicFish>();
        if (fish != null && !HasHookedObject)
        {
            source.PlayOneShot(fishCaughtSound);
        }
    }


    #region Hooking Logic
    // Called by the fish when it contacts compatible bait
    public void OnFishContact(BasicFish fish)
    {
        if (fish == null) return;

        // Double check we have compatible bait
        
        Bait currentBait = GetComponentInChildren<Bait>();
        if (currentBait != null && currentBait.IsCompatibleWithFish(fish.Size))
        {
            // Catch the fish!
            HasHookedObject = true;
            
            // Destroy the bait
            Destroy(currentBait.gameObject);

            // Tell the fish it's hooked
            fish.GetHooked();

            // Parent the fish to the hook
            fish.transform.SetParent(transform);
            fish.transform.localPosition = Vector3.zero;
            fish.transform.localRotation = Quaternion.identity;

            attachedObject = fish.gameObject;
            Debug.Log($"Caught fish: {fish.gameObject.name}");
        }
    }

    public void AttachFish(BasicFish fish)
    {
        AttractedSize = fish.Size; AttractedSize++;
        fish.GetHooked();
        AttachObject(fish.gameObject);
    }

    public void AttachBait(Bait bait)
    {
        AttractedSize = bait.baitData.targetFishSize;
        AttachObject(bait.gameObject);
    }

    private void AttachObject(GameObject obj)
    {
        if (obj == null) return;

        // Set position
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
        obj.transform.position = (Vector2)transform.position + spawnOffset + randomPoint;
        obj.transform.localRotation = Quaternion.identity;
        
        // Attach object
        if (HasHookedObject) Destroy(attachedObject); attachedObject = null;
        obj.transform.SetParent(transform);
        HasHookedObject = true;
        attachedObject = obj;
    }

    public void CatchObject()
    {
        // Runs when the fish is caught and added to cooler
        Destroy(attachedObject); attachedObject = null;
        HasHookedObject = false;
    }

    public void ReleaseObject()
    {
        // Runs when the fish is released in the water
        var attachedFish = attachedObject.GetComponent<BasicFish>();  // Reset the attached object
        if (attachedObject != null)
        {
            // Tell the fish it's released
            BasicFish fish = attachedObject.GetComponent<BasicFish>();
            if (fish != null)
            {
                fish.GetReleased();
                // Unparent and release the object
                attachedObject.transform.SetParent(null);
                attachedObject = null;
                HasHookedObject = false;
            }

            
        }
    }
    #endregion

    public void AddForce(Vector2 force)
    {
        hookRB.AddForce(force);
    }

    public void SetWaterLevel(float level)
    {
        WaterLevel = level;
    }

    public void AttachHookToSurface()
    {
        // Keep rotation vertical
        transform.rotation = Quaternion.identity;
        
        // Freeze y movement in ocean and rotation
        hookRB.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        hookRB.position = new Vector2(hookRB.position.x, WaterLevel);
        OnWaterSurface = true;

        // Play sound effect
        source.PlayOneShot(hookToWaterSound);
    }

    public void DetachHookFromSurface(float detachForce = 0f)
    {
        hookRB.constraints = RigidbodyConstraints2D.FreezeRotation;  // Keep rotation locked but allow movement
        hookRB.AddForce(Vector2.down * detachForce, ForceMode2D.Impulse);
        OnWaterSurface = false;
        //hasHookedObject = false;  // Reset hooked state when detaching from surface
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