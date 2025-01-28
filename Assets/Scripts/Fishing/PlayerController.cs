using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // BOAT FEATURES
    private FishingControls.PlayerActions playerActions;

    private BoatController boat;
    private FishingLineController line;
    private HookController hook;
    
    private void Awake()
    {
        var currTransform = gameObject.transform;
        boat = currTransform.GetChild(0).GetComponent<BoatController>();
        line = currTransform.GetChild(1).GetComponent<FishingLineController>();
        hook = currTransform.GetChild(2).GetComponent<HookController>();
        
        FishingControls fishingControls = new FishingControls();
        playerActions = fishingControls.Player;
    }

    private void Start()
    {
        playerActions.MoveBoat.Enable();
        // Change when line casting is added
        playerActions.ReelLine.Enable();
    }

    private void FixedUpdate()
    {
        // Handle input
        // Boat Movement
        var boatInput = playerActions.MoveBoat.ReadValue<float>();
        boat.SetBoatForce(boatInput);
        // Line reeling
        var reelInput = playerActions.ReelLine.ReadValue<float>();
        line.AlterLength(reelInput);

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
