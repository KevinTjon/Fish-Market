using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Market
{
    public class MarketStall : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StallConfig config;
        [SerializeField] private bool showDebugLogs = false;

        [Header("Events")]
        public UnityEvent<CustomerVisual> onCustomerArrived;
        public UnityEvent<CustomerVisual> onCustomerLeft;

        public Customer.SellerType SellerType => config.sellerType;

        private void Awake()
        {
            if (showDebugLogs)
            {
                Debug.Log($"MarketStall component added to {gameObject.name}");
            }
        }

        public void Initialize(StallConfig stallConfig)
        {
            config = stallConfig;
            
            if (showDebugLogs)
            {
                Debug.Log($"MarketStall initialized on {gameObject.name}");
            }
        }

        public void HandleCustomerArrival(CustomerVisual customer)
        {
            onCustomerArrived?.Invoke(customer);
        }

        public void HandleCustomerDeparture(CustomerVisual customer)
        {
            onCustomerLeft?.Invoke(customer);
        }

        public float GetInteractionRadius()
        {
            return config != null ? config.interactionRadius : 1.5f;
        }

        private void OnDrawGizmos()
        {
            if (config != null && config.showDebugGizmos)
            {
                // Draw interaction radius
                Gizmos.color = config.debugColor;
                Gizmos.DrawWireSphere(transform.position, config.interactionRadius);

                // Draw NPC position if not player stall
                if (config.sellerType != Customer.SellerType.Player)
                {
                    Gizmos.color = Color.green;
                    Vector3 npcPos = transform.position + (Vector3)config.npcOffset;
                    Gizmos.DrawWireSphere(npcPos, 0.3f);
                }
            }
        }
    }
} 