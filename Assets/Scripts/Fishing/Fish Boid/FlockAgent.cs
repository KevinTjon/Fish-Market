using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Requires a collider to see neighbours
[RequireComponent(typeof(Collider2D))]


public class FlockAgent : MonoBehaviour
{
    
    Flock agentFlock;
    public Flock AgentFlock{get{return agentFlock;}}
    Collider2D agentCollider;

    // allows us to get the collider without being able assign it during runtime, the only time collider should be assigned is at the start
    public Collider2D AgentCollider {get {return agentCollider;}}

    // Start is called before the first frame update
    void Start()
    {
        agentCollider = GetComponent<Collider2D>(); //Assign collider
    }

    public void Initialize(Flock flock){
        agentFlock = flock;
    }

    // turn agent towards the dirction we want, and move it to destination
    public void Move(Vector2 velocity){
        transform.up = velocity;
        transform.position += (Vector3)velocity * Time.deltaTime; //using delta time to ensure constant movement regardless of framerate of the running system
    }
}
