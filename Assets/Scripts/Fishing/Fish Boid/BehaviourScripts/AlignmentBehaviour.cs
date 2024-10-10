using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Alignment")]
public class AlignmentBehaviour : FlockBehaviour
{
    // returns the new alignment of the agent based on surround neighbours' alignment
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
            // if no neighbours, maintain current alignment
            if (context.Count == 0){
                return agent.transform.up;
            }

            //if have neighbours, add all points(where the neighbours are looking at)
            Vector2 alignmentMove = Vector2.zero;
            foreach (Transform item in context){
                alignmentMove += (Vector2)item.up;
            }
            alignmentMove /= context.Count; //find avg for all

            return alignmentMove; // new alignment for agent
    }   
}
