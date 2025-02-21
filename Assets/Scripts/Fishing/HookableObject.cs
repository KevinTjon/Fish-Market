
using UnityEngine;

public class HookableObject : MonoBehaviour, IHookable
{
    public Rigidbody2D rb { get; private set; }
    public Vector2 pos
    {
        get
        {
            return transform.position;
        }
        private set
        {
            transform.position = value;
        }
    }

    // Fish follow hook
    public void HookObject(Vector2 hookPos)
    {
        pos = hookPos;//
        //throw new System.NotImplementedException();
    }

    private void Awake()
    {
        // Instantiate Rigidbody
        rb = gameObject.GetComponent<Rigidbody2D>();
        // Sets layer
        int hookableLayer = LayerMask.NameToLayer("Hookable");
        gameObject.layer = hookableLayer;
        Debug.Log("Current layer of " + gameObject.name + ": " + gameObject.layer);
    }
}
