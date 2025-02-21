using UnityEngine;

public interface IHookable
{
    // Variables
    Rigidbody2D rb { get; }
    Vector2 pos { get; }
    
    // Functions
    void HookObject (Vector2 hookPos);
}
