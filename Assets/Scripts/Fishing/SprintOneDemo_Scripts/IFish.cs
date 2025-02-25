using UnityEngine;

public interface IFish : IHookable
{
    bool IsStunned { get; }
    bool HasBoidMovement { get; }
    Flock Flock { get; }
    
    // Initialize fish to a flock
    void Initialize(Flock flock);
    // Movement of the fish when hooked
    Vector2 HookedMovement();
    // Calculate movement of the fish
    void Move(Vector2 velocity);
}
