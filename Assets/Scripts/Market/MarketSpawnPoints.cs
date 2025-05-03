using UnityEngine;
using System.Collections.Generic;

namespace Market
{
    public class MarketSpawnPoints : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnPointMapping
        {
            public Customer.SellerType sellerType;
            public BoxCollider2D spawnPoint;
        }

        [SerializeField] private List<SpawnPointMapping> spawnPoints = new List<SpawnPointMapping>();

        public BoxCollider2D GetSpawnPoint(Customer.SellerType sellerType)
        {
            return spawnPoints.Find(x => x.sellerType == sellerType)?.spawnPoint;
        }

        private void OnValidate()
        {
            // Ensure no duplicate seller types
            var seen = new HashSet<Customer.SellerType>();
            foreach (var mapping in spawnPoints)
            {
                if (mapping.spawnPoint == null)
                {
                    Debug.LogWarning($"Missing spawn point for {mapping.sellerType}");
                    continue;
                }
                
                if (!seen.Add(mapping.sellerType))
                {
                    Debug.LogError($"Duplicate spawn point mapping for {mapping.sellerType}");
                }
            }
        }
    }
} 