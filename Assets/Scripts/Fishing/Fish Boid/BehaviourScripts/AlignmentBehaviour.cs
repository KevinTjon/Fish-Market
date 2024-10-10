using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Alignment")]
public class AlignmentBehaviour : FlockBehaviour
{
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
            // if no neighbours, maintain current alignment
            if (context.Count == 0){
                return agent.transform.up;
            }

            //if have neighbours, add all points and average
            Vector2 alignmentMove = Vector2.zero;
            foreach (Transform item in context){
                alignmentMove += (Vector2)item.up;
            }
            alignmentMove /= context.Count; //find avg

            return alignmentMove;
    }   
}
