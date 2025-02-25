using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{

    public FlockAgent agentPrefab;
    List<FlockAgent> agents = new List<FlockAgent>(); // empty list at start but once populated, allow us to iterate through all the agents
    public FlockBehaviour behaviour;

    [Range(1,500)]
    public int startingCount = 250; //populate initial flock
    const float AgentDensity = 0.08f;

    [Range(1f,100f)] //drive factor, to speed up the agent movement
    public float driveFactor = 10f;

    [Range(1f,100f)] //maxiumum speed for an agent allowed
    public float maxSpeed = 5f;

    [Range(1f,10f)] //vision view of nearby neighbours
    public float neighbourRadius = 1.5f;

    [Range(0f,1f)] 
    public float avoidanceRadiusMultiplier = 0.5f;

    [Range(1f, 10f)] // multiplier for avoidance from predator
    public float avoidancePredatorMultiplier = 2f;

    private float sqaureMaxSpeed;
    private float squareNeighbourRadius;
    private float squareAvoidanceRadius;
    public float SquareAvoidanceRadius { get {return squareAvoidanceRadius;} }

    // Start is called before the first frame update
    void Start()
    {
        // This is getting the vector's(object/agent) magnitude, which requires sqr.
        // Rather than constantly caclulating the sqaure root for every vector, compare the sqaures instead saving compuatation
        sqaureMaxSpeed = maxSpeed * maxSpeed;
        squareNeighbourRadius = neighbourRadius * neighbourRadius;
        squareAvoidanceRadius = squareNeighbourRadius * avoidanceRadiusMultiplier * avoidanceRadiusMultiplier;

        for (int i = 0; i < startingCount; i++){
            FlockAgent newAgent = Instantiate(
                agentPrefab, //prefab (object) itself
                Random.insideUnitCircle * startingCount * AgentDensity, //position of the agent to a certain size of a circle accoridng to agent density, to prevent them from spawning too far apart and ontop of each other
                Quaternion.Euler(Vector3.forward * Random.Range(0f,360f)),// set rotation from some random location from 0-360 degrees
                transform // flock's parent/transform
            );
            newAgent.name  = "Agent " + i; //creates new agents and names them accoring to the iteration
            newAgent.Initialize(this); // assigns the proper flock to this agent
            agents.Add(newAgent); //stores it to agents to allow us to access them
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (FlockAgent agent in agents)
        {
            //Vector2 move = Vector2.zero;
            if (!agent.IsStunned)
            {
                List<Transform> context = GetNearbyObjects(agent); //What things exists in our neighbour radius

                //For demo, not intended for production. INEFFICIENT, REALLY BAD WILL LAG AND EXPLODE IF TOO MUCH!!
                //agent.GetComponentInChildren<SpriteRenderer>().color = Color.Lerp(Color.white, Color.red, context.Count / 6f); //if 0 neighbours white, if 6 red, between a hue betwwen


                Vector2 move = behaviour.CalculateMove(agent, context, this);
                move *= driveFactor;
                if (move.sqrMagnitude > sqaureMaxSpeed){ //checks if move is greater than our set maxspeed
                    move = move.normalized * maxSpeed; //reset the move speed back to 1 and then set it to max speed
                }
                agent.Move(move);
            }
            //Debug.Log("Move: "+ move);
            

        }
    }

    //Rather than iterating through all the agents and find the distance compared to the radius to find our neighbours
    //using Unity physics engine, we can run a physics overlap check and just check which agent gets hit by a casted circle
    List<Transform> GetNearbyObjects(FlockAgent agent)
    {
        List<Transform> context = new List<Transform>();
        Collider2D[] contextColliders = Physics2D.OverlapCircleAll(agent.transform.position, neighbourRadius); //creates an imaginary circle in space
        
        foreach (Collider2D c in contextColliders)
        {
            if (c != agent.Collider)
            { // if not iself
                context.Add(c.transform); // add to the context list
            }
        }
        return context;
    }
}
