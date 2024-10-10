using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Avoidance")]
public class AvoidanceBehavoiur : FlockBehaviour
{
    // calculates new move based on if the agent is too close to surrounding neighbours
    // if too close to any neighbours find the mid point between all neighbours
    // this is affected by the vision range of the agent (lower, less range to see neighbours)
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
            // if no neighbours, return no adjustment. Reutrn no magnitude (no change in movement)
            if (context.Count == 0){
                return Vector2.zero;
            }

            //if have neighbours, add all points and average
            Vector2 avoidanceMove = Vector2.zero;
            int nAvoid = 0; //number of things to avoid
            foreach (Transform item in context){
                if(Vector2.SqrMagnitude(item.position-agent.transform.position) < flock.SquareAvoidanceRadius){
                    nAvoid++;
                    avoidanceMove += (Vector2)(agent.transform.position - item.position); //move away from the neighbour
                }
                
            }
            if (nAvoid > 0){ //avg of all the neighbours
                avoidanceMove /= nAvoid;
            }
            return avoidanceMove; //returns new movement
    }   
    
}