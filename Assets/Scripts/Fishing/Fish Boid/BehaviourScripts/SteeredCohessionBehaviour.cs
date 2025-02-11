using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Flock/Behaviour/Steered Cohession")]

//new version of cohessionbehaviour, fixing flickering when agents move possibly due to amount of frame skips
//using unity smooth damp to adjust the steering. Make the objects move more naturally and not flickering
public class CohessionSteeredBehaviour : FilteredFlockBehaviour
{
    Vector2 currentVelocity; // we dont use this, but smoothdamp needs this variable in its argument when doing its calculation
    public float agentSmoothTime = 0.5f; //how long an agent should take to move from current state to calculated state 


    // finds the middlepoint of all the object's neighbours and move there
    public override Vector2 CalculateMove(FlockAgent agent, List<Transform> context, Flock flock){

            //if no neighbors, return no adjustment
            if (context.Count == 0){
                return Vector2.zero;
            }

            //Debug.Log("Did Cohession ");

            //if have neighbours, add all points and average
            Vector2 cohessionMove = Vector2.zero;

            List<Transform> filteredContext = (context == null)? context: filter.Filter(agent,context);
            foreach (Transform item in filteredContext){
                cohessionMove += (Vector2)item.position;
            }
            cohessionMove /= context.Count; //find avg

            //create offset from agent postion
            cohessionMove -= (Vector2)agent.transform.position;
        
            // bugfix, a case where SmoothDamp would return a NaN vector and crash the program,
            // added a safeguard to prevent the Nan error
            if(float.IsNaN(currentVelocity.x) || float.IsNaN(currentVelocity.y)){
                currentVelocity = Vector2.zero;
            }
            //only change comparing to origianl cohessionbehaviour
            //using unity smooth damp function
            cohessionMove = Vector2.SmoothDamp(agent.transform.up, cohessionMove, ref currentVelocity, agentSmoothTime); 
            //Debug.Log("Cohession, reached end");
            //Debug.Log("Cohession "+ cohessionMove);
            return cohessionMove;
    }   
    
}
