using UnityEngine;

public class HookController : MonoBehaviour
{
    // Determines if the hook is on the water surface
    public bool onWaterSurface { get; private set; }
    public bool inSwingbackMode { get; private set; }

    public Rigidbody2D hookRB { get; private set; }
    public GameObject baitObject { get; private set; }
    
    public float baitOffset;
    public float waterLevel { get; private set; }
    
    public void InitializeHook(Rigidbody2D rb)
    {
        hookRB = rb;
        hookRB.AddForce(new Vector2(0, 0));
        
        onWaterSurface = false;
        inSwingbackMode = false;
    } 

    // Determines if the hook collides with an IHookable object
    public void OnCollisionEnter2D(Collision2D col)
    {
        if (baitObject == null)
        {
            if (col.rigidbody != null &&
                col.gameObject.layer == LayerMask.NameToLayer("Hookable"))
            {
                
                var offset = -transform.up * baitOffset;
                var newPos = offset + transform.position;
                

                // set posistion of recently hooked object
                col.gameObject.GetComponent<ObjectHookable>().Hook(newPos);

                gameObject.AddComponent<FixedJoint2D>();
                gameObject.GetComponent<FixedJoint2D>().connectedBody = col.rigidbody;
            }
        }
    }

    public void AddForce(Vector2 force)
    {
        hookRB.AddForce(force);
    }

    public void SetWaterLevel(float level)
    {
        waterLevel = level;
    }

    public void AttachHookToSurface()
    {
        // Set to ocean top
        
        // Freeze y movement in ocea
        hookRB.constraints = RigidbodyConstraints2D.FreezePositionY;
        hookRB.position = new Vector2(hookRB.position.x, waterLevel);
        onWaterSurface = true;
    }

    public void DetachHookFromSurface()
    {
        hookRB.constraints = RigidbodyConstraints2D.None;
        hookRB.AddForce(Vector2.down * 10, ForceMode2D.Impulse);
        onWaterSurface = false;
    }

}
