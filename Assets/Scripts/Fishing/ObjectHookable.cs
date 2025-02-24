using UnityEngine;

public class ObjectHookable : MonoBehaviour, IHookable
{
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected bool isHooked;

    public Rigidbody2D Rigidbody { get {return rb;} }
    public Collider2D Collider { get {return col;} }
    public bool IsHooked { get {return isHooked;} }

    public Vector2 Position
    {
        get {return transform.position;}
        protected set {transform.position = value;}
    }

    protected void Awake()
    {
        // Instantiate Rigidbody
        rb = gameObject.GetComponent<Rigidbody2D>();
        col = gameObject.GetComponent<Collider2D>();
        isHooked = false;
        // Sets layer
        gameObject.layer = LayerMask.NameToLayer("Hookable");
        Debug.Log("Current layer of " + gameObject.name + ": " + gameObject.layer);
    }

        // Fish follow hook
    public void Hook(Vector2 hookPosition)
    {
        Position = hookPosition;
        isHooked = true;
    }

    public void Unhook()
    {
        isHooked = false;
    }
}
