using UnityEngine;

namespace Market
{
    [CreateAssetMenu(fileName = "New Stall Config", menuName = "Market/Stall Configuration")]
    public class StallConfig : ScriptableObject
    {
        [Header("Stall Identity")]
        public Customer.SellerType sellerType;
        public string stallName;

        [Header("Visual Settings")]
        [Tooltip("The sprite for the NPC seller. Not used for player stalls.")]
        public Sprite npcSprite;
        [Tooltip("Position offset for the NPC sprite relative to the stall")]
        public Vector2 npcOffset = Vector2.zero;

        [Header("Interaction Settings")]
        public float interactionRadius = 1.5f;
        
        [Header("Debug Visualization")]
        public bool showDebugGizmos = true;
        public Color debugColor = new Color(0.5f, 1f, 0.5f, 0.2f);
    }
} 