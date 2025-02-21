using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Water variables
    public float waterLevel;
    
    // BOAT FEATURES
    private FishingControls.PlayerActions playerActions;
    // References to other Components/GameObjects
    private BoatController boat;
    private FishingLineController line;
    private HookController hook;
    private Animator playerAnimator;
    private Animator rodAnimator;
    
    // Player flags
    private bool isTurning;
    private bool isFacingRight;
    

    private void Awake()
    {
        var currTransform = gameObject.transform;
        var bHierarchy = currTransform.GetChild(0);
        boat = bHierarchy.GetComponent<BoatController>();
        line = currTransform.GetChild(1).GetComponent<FishingLineController>();
        hook = currTransform.GetChild(2).GetComponent<HookController>();
        
        playerAnimator = bHierarchy.GetChild(1).GetComponent<Animator>();
        rodAnimator = bHierarchy.GetChild(2).GetComponent<Animator>();
        //rodConnector = bHierarchy.GetChild(2).GetComponent<Animator>();

        
        FishingControls fishingControls = new FishingControls();
        playerActions = fishingControls.Player; 
    }

    private void Start()
    {
        playerActions.MoveBoat.Enable();
        
        // Change when line casting is added
        playerActions.ReelLine.Enable();
        // ---------------------------------

        isTurning = false;
        isFacingRight = true;
    }

    private void Update()
    {
        if (line.DoesTriggerTurn(isFacingRight) && !isTurning)
        {   
            StartCoroutine(TurnPlayer());
        }
    }

    private IEnumerator TurnPlayer()
    {
        isTurning = true;
        playerAnimator.SetTrigger("Turn");
        rodAnimator.SetTrigger("Turn");

        yield return new WaitForSeconds(0.2f);
        isFacingRight = !isFacingRight;
        boat.Flip();
        line.ResetLength();
        isTurning = false;
    }

    
    private void FixedUpdate()
    {
        // Handle input
        var boatInput = playerActions.MoveBoat.ReadValue<float>();
        var reelInput = playerActions.ReelLine.ReadValue<float>();
        
        boat.SetBoatForce(boatInput);

        if (hook.onWaterSurface)
        {
            if (reelInput >= 0)
            {
                //ReelLine();
            }
            else
            {
                hook.DetachHookFromSurface();
            }
        }
        else
        {
            line.AlterLength(reelInput);
            if (hook.hookRB.position.y > waterLevel)
            {
                line.AlterLength(-10f);
                hook.AttachHookToSurface(waterLevel);
            }
        }
        
        // Hook/Line Movement
        var tensionForce = line.CalculateHookForce();
        hook.AddForce(tensionForce);

        // Debug testing
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Object is :" + hook.baitObject);
        }
        */
    }

}
