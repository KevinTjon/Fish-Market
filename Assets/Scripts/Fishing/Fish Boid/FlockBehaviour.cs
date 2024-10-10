using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FlockBehaviour : ScriptableObject
{

    // flock agent- the object itself
    // list<Transform> - all the visible objects that surround the targeted object (neighbours)
    // flock - flock itself (the entire body)
    public abstract Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock);
}
