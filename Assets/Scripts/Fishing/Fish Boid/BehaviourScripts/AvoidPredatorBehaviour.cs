using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Avoid Predator")]
public class AvoidPredatorBehaviour : FlockBehaviour
{
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock)
    {
        Vector2 move = Vector2.zero;
        int predatorCount = 0;

        foreach (Transform item in context)
        {
            FlockAgent flockAgent = item.GetComponent<FlockAgent>();
            // Check if the agent is a predator
            if (flockAgent != null && (flockAgent.isPredator || flockAgent.IsHooked))
            { 
                predatorCount++;
                move += (Vector2)(agent.transform.position - item.position); // Move away from predator
            }

        }
        if (predatorCount > 0){
            move /= predatorCount;
            move *= flock.avoidancePredatorMultiplier;
        }
        return move;
    }
}
