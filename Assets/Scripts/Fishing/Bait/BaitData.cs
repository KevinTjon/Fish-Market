using UnityEngine;
using FishSizeNamespace;

[CreateAssetMenu(fileName = "New Bait", menuName = "Fish Market/Bait")]
public class BaitData : ScriptableObject
{
    public string baitName;
    public Color baitColor = Color.white;  // Color for the bait circle
    public ESize targetFishSize;  // The size of fish this bait attracts
} 