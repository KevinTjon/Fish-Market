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
    private RodController rod;
    private Animator playerAnimator;
    private Animator rodAnimator;

    // Player flags
    private bool isTurning;
    private bool isFacingRight;
    

    private void Awake()
    {
        var currTransform = gameObject.transform;
        var bHierarchy = currTransform.GetChild(0);
        var rHierarchy = currTransform.GetChild(1);
        boat = bHierarchy.GetComponent<BoatController>();
        rod = rHierarchy.GetComponent<RodController>();

        playerAnimator = bHierarchy.GetChild(1).GetComponent<Animator>();
        rodAnimator = bHierarchy.GetChild(2).GetComponent<Animator>();
        
        FishingControls fishingControls = new FishingControls();
        playerActions = fishingControls.Player;
        
        rod.SetWaterLevel(waterLevel);
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
        if (rod.line.DoesTriggerTurn(isFacingRight) && !isTurning)
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
        rod.line.ResetLength();
        isTurning = false;
    }

    
    private void FixedUpdate()
    {
        // Handle input
        var boatInput = playerActions.MoveBoat.ReadValue<float>();
        var reelInput = playerActions.ReelLine.ReadValue<float>();
        
        boat.SetBoatForce(boatInput);
        
        if (rod)
        {  
            rod.ReceiveReelInput(reelInput);
        }
    }
}
