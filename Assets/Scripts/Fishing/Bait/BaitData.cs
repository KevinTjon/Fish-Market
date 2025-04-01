using UnityEngine;

[CreateAssetMenu(fileName = "New Bait", menuName = "Fish Market/Bait")]
public class BaitData : ScriptableObject
{
    public string baitName;
    public Color baitColor = Color.white;  // Color for the bait circle
    public FishSize targetFishSize;  // The size of fish this bait attracts
} 