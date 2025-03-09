using UnityEngine;

[CreateAssetMenu(fileName = "Bait", menuName = "Fish/Bait")]
public class Bait : ScriptableObject
{
    [Header("Bait Properties")]
    public string baitName;
    
    [Header("Fish Type Preferences")]
    public FishSize[] targetFishSizes;
    
    [Header("Visual Settings")]
    public Color baitColor = Color.yellow;
    public float size = 0.3f;
} 