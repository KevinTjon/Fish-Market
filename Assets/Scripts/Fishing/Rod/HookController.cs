using UnityEngine;
using FishSizeNamespace;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class HookController : MonoBehaviour
{
    private float waterLevel;
    public float WaterLevel => waterLevel;
    public bool onWaterSurface { get; private set; }
    private bool isFishing; 

    [SerializeField] private float hookRadius = 0.2f;
    
    public bool hasHookedFish { get; private set; }  // Track if we have something hooked
    public FishSize attractedSize { get; private set; }
    public GameObject caughtFish { get; private set; }

    public Rigidbody2D hookRB { get; private set; }
    private CircleCollider2D hookCollider;
    private Transform rodConnection;
    private SpriteRenderer spriteRenderer;

    // Add FishSize variable to help with detection
    // Adjust the catching logic to happen here

    
    
    private void Awake()
    {
        // Setup collider
        hookCollider = GetComponent<CircleCollider2D>();
        hookCollider.radius = hookRadius;
        hookCollider.isTrigger = true;

        // Setup sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Set the hook to a specific layer
        gameObject.layer = LayerMask.NameToLayer("Hook");

        hookRB = GetComponent<Rigidbody2D>();
        hookRB.gravityScale = 0; // Disable gravity for the hook if not initialized
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
        
        hasHookedFish = false;
        onWaterSurface = false;
        isFishing = false;
    }

    // Only runs if we currently aren't fishing
    private void Update()
    {
        if (!isFishing && hookRB != null && rodConnection != null)
        {
            hookRB.position = rodConnection.position;
        }
    }

    // Called when we start fishing
    public void StartFishing(Vector2 velocity)
    {
        isFishing = true;
        hookRB.gravityScale = 1;
        hookRB.velocity = velocity;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the hook has collided with a fish
        BasicFish fish = other.GetComponent<BasicFish>();
        if (fish != null && !hasHookedFish)
        {
            // Check if the fish is compatible with the bait
            OnFishContact(fish);
        }
    }
    // Called by the fish when it contacts compatible bait
    public void OnFishContact(BasicFish fish)
    {
        if (hasHookedFish || fish == null) return;

        // Double check we have compatible bait
        Bait currentBait = GetComponentInChildren<Bait>();
        if (currentBait != null && currentBait.IsCompatibleWithFish(fish.Size))
        {
            // Catch the fish!
            hasHookedFish = true;
            
            // Destroy the bait
            Destroy(currentBait.gameObject);

            // Tell the fish it's hooked
            fish.GetHooked();

            // Parent the fish to the hook
            fish.transform.SetParent(transform);
            fish.transform.localPosition = Vector3.zero;
            fish.transform.localRotation = Quaternion.identity;

            caughtFish = fish.gameObject;
            Debug.Log($"Caught fish: {fish.gameObject.name}");
        }
    }

    public void ReleaseCaughtFish()
    {
        if (caughtFish != null)
        {
            // Tell the fish it's released
            BasicFish fish = caughtFish.GetComponent<BasicFish>();
            if (fish != null)
            {
                fish.GetReleased();
            }

            // Unparent and release the object
            caughtFish.transform.SetParent(null);
            caughtFish = null;
            hasHookedFish = false;
        }
    }

    public void AddForce(Vector2 force)
    {
        hookRB.AddForce(force);
    }

    public void SetWaterLevel(float level)
    {
        waterLevel = level;
    }

    public void AttachHookToSurface()
    {
        // Keep rotation vertical
        transform.rotation = Quaternion.identity;
        
        // Freeze y movement in ocean and rotation
        hookRB.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        hookRB.position = new Vector2(hookRB.position.x, waterLevel);
        onWaterSurface = true;
    }

    public void DetachHookFromSurface(float detachForce = 0f)
    {
        hookRB.constraints = RigidbodyConstraints2D.FreezeRotation;  // Keep rotation locked but allow movement
        hookRB.AddForce(Vector2.down * detachForce, ForceMode2D.Impulse);
        onWaterSurface = false;
        hasHookedFish = false;  // Reset hooked state when detaching from surface
    }
}