using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Market
{
    public class StallManager : MonoBehaviour
    {
        [Header("Runtime Data")]
        private List<MarketStall> activeStalls = new List<MarketStall>();

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0f, 0.5f);

        public void AddStall(MarketStall stall)
        {
            if (stall != null && !activeStalls.Any(s => s.SellerType == stall.SellerType))
            {
                activeStalls.Add(stall);
                Debug.Log($"Added stall for {stall.SellerType} with interaction point at {(stall.InteractionPoint != null ? stall.InteractionPoint.position.ToString() : "NULL")}");
            }
        }

        public void ClearStalls()
        {
            foreach (var stall in activeStalls)
            {
                if (stall != null)
                {
                    Destroy(stall.gameObject);
                }
            }
            activeStalls.Clear();
        }

        public MarketStall GetStall(Customer.SellerType sellerType)
        {
            return activeStalls.FirstOrDefault(s => s.SellerType == sellerType);
        }

        public Transform[] GetStallTransforms()
        {
            return activeStalls.Select(s => s.transform).ToArray();
        }

        public List<MarketStall> GetActiveStalls()
        {
            return activeStalls.Where(s => s != null).ToList();
        }

        public Transform GetSellerPosition(int sellerId)
        {
            var stall = GetStall((Customer.SellerType)sellerId);
            if (stall == null)
            {
                Debug.LogError($"No stall found for seller {(Customer.SellerType)sellerId}. Currently registered stalls:");
                foreach (var registeredStall in activeStalls)
                {
                    Debug.Log($"- {registeredStall.SellerType} at position {registeredStall.transform.position} with interaction point {(registeredStall.InteractionPoint != null ? registeredStall.InteractionPoint.position.ToString() : "NULL")}");
                }
                return null;
            }
            
            if (stall.InteractionPoint == null)
            {
                Debug.LogError($"Stall found for {(Customer.SellerType)sellerId} but it has no interaction point set!");
                return null;
            }
            
            return stall.InteractionPoint;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showDebugGizmos) return;

            foreach (var stall in activeStalls)
            {
                if (stall == null) continue;

                // Draw stall position and interaction radius
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube(stall.transform.position, Vector3.one);
                Gizmos.DrawWireSphere(stall.transform.position, stall.GetComponent<MarketStall>().GetInteractionRadius());
                
                // Draw interaction point
                if (stall.InteractionPoint != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(stall.InteractionPoint.position, 0.3f);
                }
            }
        }

        // Debug method to check stall registration
        public void LogRegisteredStalls()
        {
            Debug.Log("Currently registered stalls:");
            foreach (var stall in activeStalls)
            {
                if (stall != null)
                {
                    Debug.Log($"- {stall.SellerType} at position {stall.transform.position} " +
                            $"with interaction point {(stall.InteractionPoint != null ? stall.InteractionPoint.position.ToString() : "NULL")}");
                }
            }
        }
    }
} 