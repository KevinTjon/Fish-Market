using UnityEngine;
using Market;
using System.Linq;
using UnityEditor;

namespace Market
{
    public class SimpleSellerSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StallConfig[] stallConfigs;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private StallManager stallManager;
        [SerializeField] private Vector2 defaultSpawnOffset = Vector2.right * 2f; // Space between stalls
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private void Start()
        {
            ValidateSetup();
            if (spawnOnStart)
            {
                SpawnAllSellers();
            }
        }

        private void ValidateSetup()
        {
            if (stallManager == null)
            {
                stallManager = GetComponent<StallManager>();
                if (stallManager == null)
                {
                    stallManager = FindObjectOfType<StallManager>();
                    if (stallManager == null)
                    {
                        Debug.LogError("No StallManager found in scene! Please add one.");
                        enabled = false;
                        return;
                    }
                }
            }

            if (stallConfigs == null || stallConfigs.Length == 0)
            {
                Debug.LogWarning("No stall configurations assigned to SimpleSellerSpawner!");
            }
        }

        public void SpawnAllSellers()
        {
            // Clear existing stalls first
            stallManager.ClearStalls();
            
            // Register player's stall first (assuming it's already in the scene)
            RegisterPlayerStall();
            
            // Then spawn NPC stalls
            Vector2 currentSpawnPos = transform.position;
            foreach (var config in stallConfigs)
            {
                if (config.sellerType != Customer.SellerType.Player)
                {
                    SpawnSellerStall(config, currentSpawnPos);
                    currentSpawnPos += defaultSpawnOffset;
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"Spawned {stallConfigs.Length} seller stalls successfully.");
            }
        }

        private void RegisterPlayerStall()
        {
            var playerStallObj = GameObject.FindGameObjectWithTag("PlayerStall");
            if (playerStallObj != null)
            {
                var playerConfig = stallConfigs.FirstOrDefault(c => c.sellerType == Customer.SellerType.Player);
                if (playerConfig != null)
                {
                    var marketStall = playerStallObj.GetComponent<MarketStall>();
                    if (marketStall == null)
                    {
                        marketStall = playerStallObj.AddComponent<MarketStall>();
                    }
                    
                    marketStall.Initialize(playerConfig);
                    stallManager.AddStall(marketStall);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log("Registered player's stall successfully.");
                    }
                }
                else
                {
                    Debug.LogError("No configuration found for player stall!");
                }
            }
            else
            {
                Debug.LogError("Player's stall not found in scene! Tag it with 'PlayerStall'");
            }
        }

        private void SpawnSellerStall(StallConfig config, Vector2 position)
        {
            if (config == null) return;

            // Create main stall object at the provided position
            GameObject stallObj = new GameObject($"Stall_{config.stallName}");
            stallObj.transform.parent = transform;
            stallObj.transform.position = position;
            
            // Add MarketStall component and initialize
            var marketStall = stallObj.AddComponent<MarketStall>();
            marketStall.Initialize(config);
            
            stallManager.AddStall(marketStall);

            if (showDebugInfo)
            {
                Debug.Log($"Spawned stall for seller type {config.sellerType} at position {position}");
            }
        }

        public void DespawnAllSellers()
        {
            stallManager.ClearStalls();
            if (showDebugInfo)
            {
                Debug.Log("Despawned all sellers.");
            }
        }

        public StallConfig GetStallConfig(Customer.SellerType sellerType)
        {
            return stallConfigs?.FirstOrDefault(c => c.sellerType == sellerType);
        }

        public StallConfig[] GetAllConfigs()
        {
            return stallConfigs;
        }
    }
} 