using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookController : MonoBehaviour
{
    [SerializeField] private FishingLineController line;

    public bool inSwingbackMode {get; private set;}

    public Rigidbody2D hookRB {get; private set;}
    
    // Start is called before the first frame update
    void Start()
    {
        hookRB = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
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
