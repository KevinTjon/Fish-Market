using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Composite")]

// compositing all 3 behaviours together to allow agent to do all three
public class CompositeBehaviour : FlockBehaviour
{
    

    public FlockBehaviour[] behaviours; // behaviours stored here
    public float[] weights; // the intensity of each behaviour based on the array i in behaviours
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
        // handle datamistach with weights and number of behaviours. Should not run, setup error
        if (weights.Length != behaviours.Length){
            Debug.LogError("Data mismatch in " + name, this); // setup error
            return Vector2.zero;
        }

        //setup move
        Vector2 move = Vector2.zero; // initial movement change, set to 0 currently
        //iterate through all behaviours
        for(int i = 0; i < behaviours.Length; i++){
            Vector2 partialMove = behaviours[i].CalculateMove(agent, context, flock) * weights[i]; //act as a middleman to pass these arguments to the actual behaviour scritps

            if (partialMove != Vector2.zero){ // if it returns some new movement (not 0)
                //checks if this overall movement exceeds the weight
                if(partialMove.sqrMagnitude > weights[i] * weights[i]){
                    partialMove.Normalize(); //set to 0
                    partialMove *= weights[i]; //then set move to the set max
                }
                move += partialMove; //add calculated movement for all behaviours (can be negative)
            }
        }
        return move; // returns the final movement change for the agent
    }
}
