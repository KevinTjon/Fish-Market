using UnityEngine;

public class HookController : MonoBehaviour
{
    // Determines if the hook is on the water surface
    public bool onWaterSurface { get; private set; }
    public bool inSwingbackMode { get; private set; }
    public bool hasHookedObject { get; private set; }  // Track if we have something hooked

    public Rigidbody2D hookRB { get; private set; }
    public GameObject baitObject { get; private set; }
    public BaitObject currentBait { get; private set; }
    
    public float baitOffset = 0.5f;
    public float waterLevel { get; private set; }
    
    private CircleCollider2D hookCollider;
    private float targetRotation = 0f;
    private float rotationSmoothSpeed = 5f;
    private Transform rodTransform;
    
    private void Awake()
    {
        // Get or add a small collider for the hook
        hookCollider = GetComponent<CircleCollider2D>();
        if (hookCollider == null)
        {
            hookCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        // Make the hook collider larger and ensure it's not a trigger
        hookCollider.radius = 0.3f;
        hookCollider.isTrigger = false;
        
        // Set the hook to the Hook layer (we'll need to create this layer in Unity)
        gameObject.layer = LayerMask.NameToLayer("Hook");
        
        // Initialize hooked state
        hasHookedObject = false;
    }
    
    public void InitializeHook(Rigidbody2D rb)
    {
        hookRB = rb;
        hookRB.AddForce(new Vector2(0, 0));
        
        // Configure rigidbody for better hook behavior
        hookRB.angularDrag = 2f;
        hookRB.drag = 0.5f;
        // Lock rotation to keep hook vertical
        hookRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Get reference to rod
        rodTransform = transform.parent.parent.GetChild(0).GetChild(3);
        
        // Ensure the Hook layer exists and set up layer collision matrix
        SetupHookLayer();
        
        onWaterSurface = false;
        inSwingbackMode = false;
    }

    private void SetupHookLayer()
    {
        // This is just a debug check - layer setup should be done in Unity Editor
        if (LayerMask.NameToLayer("Hook") == -1)
        {
            Debug.LogWarning("Hook layer not found! Please create a 'Hook' layer in Unity:\n" +
                           "1. Edit > Project Settings > Tags and Layers\n" +
                           "2. Under 'Layers', add 'Hook' in an empty slot\n" +
                           "3. Set up the Physics2D collision matrix to allow Hook to collide with Hookable");
        }
    }

    private void FixedUpdate()
    {
        if (hookRB != null)
        {
            // Keep hook vertical at all times
            transform.rotation = Quaternion.identity;

            /* Original rotation logic
            // Calculate rotation based on line direction instead of velocity
            Vector2 lineDirection = ((Vector2)(rodTransform.position - transform.position)).normalized;
            
            // Only update rotation if we're moving enough
            if (lineDirection.magnitude > 0.01f)
            {
                targetRotation = Mathf.Atan2(lineDirection.y, lineDirection.x) * Mathf.Rad2Deg + 90f;
            }
            
            // Smoothly rotate towards target rotation
            float currentRotation = transform.eulerAngles.z;
            if (currentRotation > 180) currentRotation -= 360;
            
            float newRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.fixedDeltaTime * rotationSmoothSpeed);
            transform.rotation = Quaternion.Euler(0, 0, newRotation);
            
            // Keep the hook's rotation constrained when on surface
            if (onWaterSurface)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            */
        }
    }

    public void AttachBait(GameObject baitPrefab)
    {
        // Remove any existing bait
        if (baitObject != null)
        {
            Destroy(baitObject);
        }

        // Create and attach new bait
        if (baitPrefab != null)
        {
            // Create bait at hook's position with offset
            Vector3 baitPosition = transform.position - transform.up * baitOffset;
            baitObject = Instantiate(baitPrefab, baitPosition, Quaternion.identity);
            currentBait = baitObject.GetComponent<BaitObject>();
            
            // Debug log bait data
            if (currentBait != null && currentBait.baitData != null)
            {
                string targetSizes = currentBait.baitData.targetFishSizes != null ? 
                    string.Join(", ", currentBait.baitData.targetFishSizes) : "none";
                Debug.Log($"Attached bait: {currentBait.baitData.name} with target sizes: [{targetSizes}]");
            }
            else
            {
                Debug.LogError($"Failed to load bait data for {baitPrefab.name}");
            }
            
            // Parent the bait to the hook
            baitObject.transform.SetParent(transform);
            baitObject.transform.localPosition = -Vector3.up * baitOffset;
            baitObject.transform.localRotation = Quaternion.identity;
        }
    }

    public void RemoveBait()
    {
        if (baitObject != null)
        {
            Destroy(baitObject);
            baitObject = null;
            currentBait = null;
        }
    }

    // Determines if the hook collides with an IHookable object
    public void OnCollisionEnter2D(Collision2D col)
    {
        // If we already have something hooked, ignore new collisions
        if (hasHookedObject) return;
        
        // Check if we're colliding with a hookable object
        if (col.collider != null && col.gameObject.layer == LayerMask.NameToLayer("Hookable"))
        {
            var hookable = col.gameObject.GetComponent<ObjectHookable>();
            var fishHookable = hookable as FishHookable;  // Try to cast to FishHookable
            
            // Only proceed if:
            // 1. We have a valid hookable
            // 2. It's not already hooked
            // 3. Either:
            //    a) It's not a fish, OR
            //    b) We have compatible bait and the fish is attracted to our bait type
            if (hookable != null && !hookable.IsHooked && 
                (fishHookable == null || 
                 (currentBait != null && IsBaitCompatibleWithFish(fishHookable) && currentBait.IsAttractedToFish(GetFishSize(fishHookable)))))
            {
                Debug.Log($"Attempting to hook: {col.gameObject.name}"); // Debug log
                
                // Calculate hook position based on collision point
                Vector2 collisionPoint = col.GetContact(0).point;
                Vector2 hookPos = transform.position;
                Vector2 direction = (collisionPoint - hookPos).normalized;
                
                // Hook position is slightly offset from collision point
                Vector2 newPos = collisionPoint - direction * 0.2f;
                
                // Hook the object
                hookable.Hook(newPos);
                hasHookedObject = true;  // Mark that we have something hooked
                
                // Remove bait if we caught something
                RemoveBait();
                
                // Remove any existing joints
                var joints = GetComponents<Joint2D>();
                foreach (var joint in joints)
                {
                    Destroy(joint);
                }
                
                // Add joint to connect the caught object
                var newJoint = gameObject.AddComponent<FixedJoint2D>();
                newJoint.connectedBody = col.rigidbody;
                newJoint.autoConfigureConnectedAnchor = false;
                newJoint.anchor = Vector2.zero;
                newJoint.connectedAnchor = col.transform.InverseTransformPoint(collisionPoint);
                
                Debug.Log($"Successfully hooked: {col.gameObject.name}"); // Debug log
            }
            else
            {
                // Debug why hooking failed
                if (hookable == null) Debug.Log("Failed to hook: No hookable component");
                else if (hookable.IsHooked) Debug.Log("Failed to hook: Already hooked");
                else if (fishHookable != null)
                {
                    if (currentBait == null) Debug.Log("Failed to hook: No bait attached");
                    else if (!IsBaitCompatibleWithFish(fishHookable)) Debug.Log("Failed to hook: Incompatible bait type");
                    else if (!currentBait.IsAttractedToFish(GetFishSize(fishHookable))) Debug.Log($"Failed to hook: Fish size {GetFishSize(fishHookable)} not attracted to this bait");
                }
            }
        }
    }
    
    private FishSize GetFishSize(FishHookable fish)
    {
        // Get the fish size from its behavior
        if (fish == null || fish.behavior == null)
            return FishSize.Medium; // Default to medium if no behavior
        
        float size = fish.behavior.size;
        if (size <= 0.6f) return FishSize.Tiny;
        else if (size <= 0.8f) return FishSize.Small;
        else if (size <= 1.2f) return FishSize.Medium;
        else if (size <= 1.7f) return FishSize.Large;
        else return FishSize.Huge;
    }

    private bool IsBaitCompatibleWithFish(FishHookable fish)
    {
        // If we have no bait, nothing can be caught
        if (currentBait == null || currentBait.baitData == null || fish == null || fish.behavior == null)
        {
            Debug.LogWarning($"Bait compatibility check failed: currentBait={currentBait != null}, baitData={currentBait?.baitData != null}, fish={fish != null}, behavior={fish?.behavior != null}");
            return false;
        }
        
        // Get the fish size using the existing GetFishSize method
        FishSize fishSize = GetFishSize(fish);
        
        Debug.Log($"Fish size check - Raw size: {fish.behavior.size}, Calculated size: {fishSize}");
        
        // Check if this fish size is compatible with our bait
        bool isCompatible = currentBait.IsAttractedToFish(fishSize);
        if (!isCompatible)
        {
            Debug.Log($"Bait {currentBait.baitData.name} is not compatible with fish size {fishSize}");
        }
        
        return isCompatible;
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
        //targetRotation = 0f;  // Commented out as part of rotation logic
        
        // Freeze y movement in ocean and rotation
        hookRB.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        hookRB.position = new Vector2(hookRB.position.x, waterLevel);
        onWaterSurface = true;
    }

    public void DetachHookFromSurface()
    {
        hookRB.constraints = RigidbodyConstraints2D.FreezeRotation;  // Keep rotation locked but allow movement
        hookRB.AddForce(Vector2.down * 10, ForceMode2D.Impulse);
        onWaterSurface = false;
        hasHookedObject = false;  // Reset hooked state when detaching from surface
    }
}
