using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[CreateAssetMenu(menuName = "Flock/Behaviour/Avoidance")]
public class AvoidanceBehavoiur : FilteredFlockBehaviour
{
    // calculates new move based on if the agent is too close to surrounding neighbours
    // if too close to any neighbours find the mid point between all neighbours
    // this is affected by the vision range of the agent (lower, less range to see neighbours)
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        //int nAvoid = 0; // number of obstacles to avoid
        // if no neighbours, return no adjustment. Reutrn no magnitude (no change in movement)
        Vector2 avoidanceMove = Vector2.zero;
        
        if (context.Count == 0)
        {
            //Debug.Log("Avoidance "+ move);
            return avoidanceMove;
        }
        
        int nNeighbourAvoid = 0; // number of things to avoid

        List<Transform> filteredContext = (filter == null)? context: filter.Filter(agent,context);

        foreach (Transform item in filteredContext)
        {
            Vector3 closestPoint = item.gameObject.GetComponent<Collider2D>().ClosestPoint(agent.transform.position);
            if (Vector2.SqrMagnitude(closestPoint - agent.transform.position) < flock.SquareAvoidanceRadius)
            {
                nNeighbourAvoid++;
                avoidanceMove += (Vector2)(agent.transform.position - closestPoint); // move away from the neighbour
            }
        }
        
        if (nNeighbourAvoid > 0) //avg of all the neighbours
        { 
            avoidanceMove /= nNeighbourAvoid;
        }
        
        return avoidanceMove; //returns new movement

    }
}