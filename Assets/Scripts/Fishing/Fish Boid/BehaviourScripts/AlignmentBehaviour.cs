using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Alignment")]
public class AlignmentBehaviour : FilteredFlockBehaviour
{
    // returns the new alignment of the agent based on surround neighbours' alignment
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
            // if no neighbours, maintain current alignment
            if (context.Count == 0){
                //Debug.Log("Alignment "+ agent.transform.up);
                return agent.transform.up;
            }

            //if have neighbours, add all points(where the neighbours are looking at)
            Vector2 alignmentMove = Vector2.zero;
            List<Transform> filteredContext = (context == null)? context: filter.Filter(agent,context);
            foreach (Transform item in filteredContext){
                alignmentMove += (Vector2)item.up;
            }
            alignmentMove /= context.Count; //find avg for all

            //Debug.Log("Alignment "+ alignmentMove);
            return alignmentMove; // new alignment for agent
    }   
}
