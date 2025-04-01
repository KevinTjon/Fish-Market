using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class SimpleHookController : MonoBehaviour
{
    [SerializeField] private float hookRadius = 0.2f;
    [SerializeField] private float hookOffset = 0.5f; // Distance fish follows below hook
    
    private CircleCollider2D hookCollider;
    private SpriteRenderer spriteRenderer;
    private bool hasCaughtFish = false;
    private GameObject caughtObject;

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
    }

    // Called by the fish when it contacts compatible bait
    public void OnFishContact(BasicFish fish)
    {
        if (hasCaughtFish || fish == null) return;

        // Double check we have compatible bait
        Bait currentBait = GetComponentInChildren<Bait>();
        if (currentBait != null && currentBait.IsCompatibleWithFish(fish.FishSize))
        {
            // Catch the fish!
            hasCaughtFish = true;
            
            // Destroy the bait
            Destroy(currentBait.gameObject);

            // Tell the fish it's hooked
            fish.GetHooked();

            // Parent the fish to the hook and position it below
            fish.transform.SetParent(transform);
            fish.transform.localPosition = Vector3.down * hookOffset;
            fish.transform.localRotation = Quaternion.identity;

            caughtObject = fish.gameObject;
            Debug.Log($"Caught fish: {fish.gameObject.name}");
        }
    }

    public void ReleaseCaughtObject()
    {
        if (caughtObject != null)
        {
            // Tell the fish it's released
            BasicFish fish = caughtObject.GetComponent<BasicFish>();
            if (fish != null)
            {
                fish.GetReleased();
            }

            // Unparent and release the object
            caughtObject.transform.SetParent(null);
            caughtObject = null;
            hasCaughtFish = false;
        }
    }
} 