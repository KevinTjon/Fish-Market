using UnityEngine;

public class HookController : MonoBehaviour
{
    // Determines if the hook is on the water surface
    public bool onWaterSurface { get; private set; }
    public bool inSwingbackMode { get; private set; }
    public Rigidbody2D hookRB { get; private set; }
    public GameObject baitObject { get; private set; }
    
    public float baitOffset;
    public float waterLevel;
    
    // Start is called before the first frame update
    void Start()
    {
        hookRB = GetComponent<Rigidbody2D>();
        hookRB.AddForce(new Vector2(0, 0));

        onWaterSurface = false;
        inSwingbackMode = false;
    }
    // Update is called once per frame
    void Update()
    {
        // Change color of line based on strength
    }

    // Determines if the hook collides with an IHookable object
    public void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (baitObject == null)
        {
            if (collision2D.rigidbody != null &&
                collision2D.gameObject.layer == LayerMask.NameToLayer("Hookable"))
            {
                var offset = -transform.up * baitOffset;
                var newPos = offset + transform.position;
                collision2D.gameObject.GetComponent<HookableObject>().HookObject(newPos);

                gameObject.AddComponent<FixedJoint2D>();
                gameObject.GetComponent<FixedJoint2D>().connectedBody = collision2D.rigidbody;
            }
        }
    }

    public void AddForce(Vector2 force)
    {
        hookRB.AddForce(force);
    }

    public void SetSwingbackMode(bool mode)
    {
        inSwingbackMode = mode;
    }

    public void AttachHookToSurface(float posY)
    {
        // Set to ocean top
        hookRB.position = new Vector2(hookRB.position.x, posY);
        // Freeze y movement in ocea
        hookRB.constraints = RigidbodyConstraints2D.FreezePositionY;
        onWaterSurface = true;
    }

    public void DetachHookFromSurface()
    {
        hookRB.constraints = RigidbodyConstraints2D.None;
        hookRB.AddForce(Vector2.down * 10, ForceMode2D.Impulse);
        onWaterSurface = false;
    }

}
