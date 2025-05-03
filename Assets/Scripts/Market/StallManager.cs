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
            }
        }
    }
} 