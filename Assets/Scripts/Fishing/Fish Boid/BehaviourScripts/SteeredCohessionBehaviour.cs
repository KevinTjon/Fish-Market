using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Steered Cohession")]

//new version of cohessionbehaviour, due to unwanted flickering when agents move
//using unity smooth damp to adjust the steering. Make the objects move more naturally and not flickering
public class CohessionSteeredBehaviour : FlockBehaviour
{
    Vector2 currentVelocity; // we dont use this, but smoothdamp needs this variable in its argument when doing its calculation
    public float agentSmoothTime = 0.5f; //how long an agent should take to move from current state to calculated state 


    // finds the middlepoint of all the object's neighbours and move there
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){
            // if no neighbours, return no adjustment. Reutrn no magnitude
            if (context.Count == 0){
                return Vector2.zero;
            }

            //if have neighbours, add all points and average
            Vector2 cohessionMove = Vector2.zero;
            foreach (Transform item in context){
                cohessionMove += (Vector2)item.position;
            }
            cohessionMove /= context.Count; //find avg

            //create offset from agent postion
            cohessionMove -= (Vector2)agent.transform.position;
            //only change comparing to origianl cohessionbehaviour
            //using unity smooth damp function
            cohessionMove = Vector2.SmoothDamp(agent.transform.up, cohessionMove, ref currentVelocity, agentSmoothTime); 
            return cohessionMove;
    }   
    
}
