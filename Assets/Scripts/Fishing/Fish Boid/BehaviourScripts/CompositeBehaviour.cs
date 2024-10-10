using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Composite")]

// compositing all 3 behaviours together
public class CompositeBehaviour : FlockBehaviour
{
    

    public FlockBehaviour[] behaviours;
    public float[] weights;
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
        // handle datamistach with weights and number of behaviours
        if (weights.Length != behaviours.Length){
            Debug.LogError("Data mismatch in " + name, this);
            return Vector2.zero;
        }

        //setup move
        Vector2 move = Vector2.zero;
        //iterate through behaviours
        for(int i = 0; i < behaviours.Length; i++){
            Vector2 partialMove = behaviours[i].CalculateMove(agent, context, flock) * weights[i]; //act as a middleman to pass these arguments to the actual behaviour scrips

            if (partialMove != Vector2.zero){ // if it returns some movement
                //checks if this overall movement exceeds the weight
                if(partialMove.sqrMagnitude > weights[i] * weights[i]){
                    partialMove.Normalize();
                    partialMove *= weights[i];
                }
                move += partialMove;
            }
        }
        return move;
    }
}
