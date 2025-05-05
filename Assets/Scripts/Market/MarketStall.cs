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
        [SerializeField] private Transform interactionPoint;

        [Header("Events")]
        public UnityEvent<Customer> onCustomerArrived;
        public UnityEvent<Customer> onCustomerLeft;

        public Customer.SellerType SellerType => config.sellerType;
        public Transform InteractionPoint => interactionPoint;

        private void Awake()
        {
            if (showDebugLogs)
            {
                Debug.Log($"MarketStall component added to {gameObject.name}");
            }

            if (interactionPoint == null)
            {
                Debug.LogWarning($"No interaction point set for {gameObject.name}. Using stall transform instead.");
                interactionPoint = transform;
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

        public void HandleCustomerArrival(Customer customer)
        {
            onCustomerArrived?.Invoke(customer);
        }

        public void HandleCustomerDeparture(Customer customer)
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
            }
        }
    }
} 