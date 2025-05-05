using UnityEngine;

namespace Market
{
    [CreateAssetMenu(fileName = "New Stall Config", menuName = "Market/Stall Configuration")]
    public class StallConfig : ScriptableObject
    {
        [Header("Stall Identity")]
        public Customer.SellerType sellerType;
        public string stallName;

        [Header("Interaction Settings")]
        public float interactionRadius = 1.5f;
        
        [Header("Debug Visualization")]
        public bool showDebugGizmos = true;
        public Color debugColor = new Color(0.5f, 1f, 0.5f, 0.2f);
    }
} 