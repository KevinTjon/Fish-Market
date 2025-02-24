using System.Collections.Generic;
using UnityEngine;

public abstract class FlockBehaviour : ScriptableObject
{
    // Flock agent- the object itself
    // List<Transform> context - all the visible objects that surround the targeted object (neighbours)
    // Flock flock - flock itself (the entire body)
    public abstract Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock);
}
