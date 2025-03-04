using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Stay In Radius")]
public class StayInRadiusBehaviour : FlockBehaviour
{

    public Vector2 center;
    public float radius = 15f;

    //VERY BUGGY RIGHT NOW, THIS METHOD MIGHT NOT BE NEEDED IF USING CHUNK LIMITER IDEA

    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        Vector2 centerOffset = center - (Vector2)agent.transform.position;
        float t = centerOffset.magnitude / radius;
        if (t < 0.9f){
            return Vector2.zero;
        }

        //Debug.Log("Radius "+ centerOffset * t * t);
        return centerOffset * t * t;
    }

}
