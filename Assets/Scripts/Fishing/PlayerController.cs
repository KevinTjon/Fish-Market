using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private FishingLineController line;
    private HookController hook;
    
    //[SerializeField] private Rigidbody2D hookRB;

    [SerializeField] private Transform rodPoint;
    [SerializeField] private Transform hookPoint;

    // Controls
    [SerializeField] private InputActionAsset playerControls;
    // Start is called before the first frame update
    
    private void Awake()
    {
        
    }
    private void Start()
    {
        var currTransform = gameObject.transform;

        line = currTransform.GetChild(1).GetComponent<FishingLineController>();
        hook = currTransform.GetChild(2).GetComponent<HookController>();
        //line.CreateLine(rodPoint, hookPoint);
    }

    void FixedUpdate()
    {
        var tensionForce = line.CalculateTension();
        hook.AddForce(tensionForce);
    }

    private void CalculatePhysics()
    {
        // Get hook posiiton
        // Get Rod Position
        // Calculate Tension
    }

    private void MoveBoat()
    {

    }

    //private

}
