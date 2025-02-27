using UnityEngine;

public interface IFish : IHookable
{
    bool IsStunned { get; }
    
    // Movement of the fish when hooked
    Vector2 HookedMovement();
    // Calculate movement of the fish
    void Move(Vector2 velocity);
}
