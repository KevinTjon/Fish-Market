using UnityEngine;

[CreateAssetMenu(fileName = "Bait", menuName = "Fish/Bait")]
public class Bait : ScriptableObject
{
    [Header("Bait Properties")]
    public string baitName;
    public float attractionRadius = 3f;
    public float attractionStrength = 1.5f;
    
    [Header("Fish Type Preferences")]
    public FishSize[] targetFishSizes;
    [Range(0f, 3f)]
    public float sizeSpecificAttractionMultiplier = 1.5f;
    
    [Header("Visual Settings")]
    public Color baitColor = Color.yellow;
    public float size = 0.3f;
} 