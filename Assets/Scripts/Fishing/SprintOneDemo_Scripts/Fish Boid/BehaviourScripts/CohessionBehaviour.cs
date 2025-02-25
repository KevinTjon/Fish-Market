using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Cohession")]
public class CohessionBehaviour : FilteredFlockBehaviour
{
    // finds the middlepoint of all the object's neighbours and move there
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
            // if no neighbours, return no adjustment. Reutrn no magnitude
            if (context.Count == 0){
                return Vector2.zero;
            }

            //if have neighbours, add all points and average
            Vector2 cohessionMove = Vector2.zero;
            List<Transform> filteredContext = (filter == null)? context: filter.Filter(agent,context);
            foreach (Transform item in filteredContext){
                cohessionMove += (Vector2)item.position;
            }
            cohessionMove /= context.Count; //find avg

            //create offset from agent postion
            cohessionMove -= (Vector2)agent.transform.position;
            return cohessionMove;
    }   
    
}
