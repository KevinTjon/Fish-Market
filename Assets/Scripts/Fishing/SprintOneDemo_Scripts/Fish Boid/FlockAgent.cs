using System.Collections;
using UnityEngine;

//Requires a collider to see neighbours
[RequireComponent(typeof(Collider2D))]

public class FlockAgent : ObjectHookable, IFish
{
    private bool isStunned;
    public bool IsStunned { get {return isStunned;}}
    public bool HasBoidMovement { get {return !isStunned && !isHooked;} }
    
    private Flock flock;
    public Flock Flock { get {return flock;} }

    public bool isPredator;

    private new void Awake()
    {
        base.Awake();
        rb.gravityScale = 0;
        //rb.isKinematic = true;
        isHooked = false;
        isStunned = false;
    }

    // Removes item from the boid algorithm, but also points 
    public new void Hook(Vector2 hookPos)
    {
        base.Hook(hookPos);
        isStunned = true;
        rb.gravityScale = 0;
        //rb.isDynamic = false;
    }

    // Unhooks fish, then stuns for a bit
    public new void Unhook()
    {
        StartCoroutine(UnhookFishStun());
    }

    private IEnumerator UnhookFishStun()
    {
        // Wait for fish to regain control
        isHooked = false;
        yield return new WaitForSeconds(2f);
        isStunned = false;
        rb.gravityScale = 1;
    }

    public void Initialize(Flock flock)
    {
        this.flock = flock;
    }
    
    public Vector2 HookedMovement()
    {
        // Write a function to automatically add movement algorithm
        throw new System.NotImplementedException();
    }

    public void Move(Vector2 velocity)
    {
        if (!isStunned)
        {
            
            transform.up = velocity;
            rb.velocity = velocity;
            //transform.position += (Vector3)velocity * Time.deltaTime; //using delta time to ensure constant movement regardless of framerate of the running system
            // *deltaTime gets the time between frames
            // *fixedDeltaTime gets the time between physics simulations

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
}
