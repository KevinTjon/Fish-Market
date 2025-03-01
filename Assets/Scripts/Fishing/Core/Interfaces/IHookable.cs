using UnityEngine;

public interface IHookable
{
    // Variables
    Rigidbody2D Rigidbody { get; }
    Collider2D Collider { get; }
    Vector2 Position { get; }
    bool IsHooked { get; }
    
    // Functions
    void Hook (Vector2 hookPos);
    void Unhook();
}
