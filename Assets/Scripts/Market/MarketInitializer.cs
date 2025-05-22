using UnityEngine;

namespace Market
{
    public class MarketInitializer : MonoBehaviour
    {
        [SerializeField] private StallManager stallManager;

        private void Awake()
        {
            // Find StallManager if not assigned
            if (stallManager == null)
            {
                stallManager = FindObjectOfType<StallManager>();
                if (stallManager == null)
                {
                    Debug.LogError("No StallManager found in scene!");
                    return;
                }
            }

            // Find all MarketStall components in the scene
            MarketStall[] allStalls = FindObjectsOfType<MarketStall>();
            Debug.Log($"Found {allStalls.Length} stalls in scene");

            // Register each stall with the StallManager
            foreach (MarketStall stall in allStalls)
            {
                stallManager.AddStall(stall);
                Debug.Log($"Registered stall {stall.gameObject.name} with type {stall.SellerType}");
            }

            // Log all registered stalls for verification
            stallManager.LogRegisteredStalls();
        }
    }
} 