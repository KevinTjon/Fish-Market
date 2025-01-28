using UnityEngine;

public class HookController : MonoBehaviour
{
    private const float Damping = 40f;
    
    public bool inSwingbackMode { get; private set; }
    public Rigidbody2D hookRB { get; private set; }
    public GameObject baitObject { get; private set; }
    public float baitOffset;
    
    // Start is called before the first frame update
    void Start()
    {
        hookRB = GetComponent<Rigidbody2D>();
        hookRB.AddForce(new Vector2(0, 0));
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
}
