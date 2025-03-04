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

    public bool isPredator;
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
    // this combines all the behaviours we want for the fish and makes a final move
    public void Move(Vector2 velocity){
        transform.up = velocity;
        transform.position += (Vector3)velocity * Time.deltaTime; //using delta time to ensure constant movement regardless of framerate of the running system

        // Get the child sprite object
        Transform spriteTransform = transform.Find("Sprite"); //finds the child sprite renderer

        // Flip the sprite based on the movement direction
        if (velocity.x > 0)
        {
            if (spriteTransform != null)
            {
                spriteTransform.localScale = new Vector3(1, 1, 1); // Face right
            }
        }
        else if (velocity.x < 0)
        {
            if (spriteTransform != null)
            {
                spriteTransform.localScale = new Vector3(-1, 1, 1); // Face left
            }
        }
        // If moving up or down, do not change the scale

        // Restrict rotation of the child sprite
        if (spriteTransform != null)
        {
            spriteTransform.rotation = Quaternion.identity; // Reset rotation to prevent any rotation
        }
        
    }
}
