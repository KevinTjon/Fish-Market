using UnityEngine;

public enum FishSize
{
    Tiny,
    Small,
    Medium,
    Large,
    Huge
}

[CreateAssetMenu(fileName = "FishType", menuName = "Fish/Type")]
public class FishType : ScriptableObject
{
    public string fishName;
    public FishSize size;
    public float value; // The monetary value of the fish
    public bool canEat; // Whether this fish can eat other fish
    public FishSize[] preySize; // What sizes of fish this can eat
} 