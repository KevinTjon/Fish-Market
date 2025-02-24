using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Filter/Same Flock")]

public class SameFlockFilter : ContextFilter
{
    public override List<Transform> Filter(FlockAgent agent, List<Transform> original)
    {
        List<Transform> filtered = new List<Transform>();
        foreach(Transform item in original)
        {
            FlockAgent itemAgent = item.GetComponent<FlockAgent>();
            // if has a flock agent and the agent is in the same flock as item agent
            if (itemAgent != null && itemAgent.Flock == agent.Flock)
            { 
                filtered.Add(item); 
            }
        }
        return filtered;
    }
}
