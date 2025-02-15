using UnityEngine;

public interface IFish : IHookable
{
    // Default movement of the fish in ocean environment
    Vector2 DefaultMovement { get; }
    // Movement of the fish when hooked
    Vector2 HookedMovement { get; }
}
