using UnityEngine;

public class BoatController : MonoBehaviour
{
    // Intrinsic Attributes (Add to ScriptableObject later)
    private const float BoatSpeed = 2500f;


    // Physical Attribute
    public Rigidbody2D rb {get; private set;}
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetBoatForce(float rawInput)
    {
        rb.AddForce(new Vector2(rawInput * BoatSpeed, 0));
        // TODO: Add feature to limit boat speed
    }
}
