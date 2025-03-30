using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    // UI Variables
    private readonly float pauseTime = 0.3f;
    //private FishingControls.UIActions uiActions;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject inventoryUI;

    // Player flags
    private bool isTurning;
    private bool isFacingRight;
    private bool isPaused;
    
    // Public get variable for isPaused for UI screens
    public bool IsPaused { get => isPaused; }

    private void Awake()
    {
        var currTransform = gameObject.transform;
        var bHierarchy = currTransform.GetChild(0);
        var rHierarchy = currTransform.GetChild(1);
        boat = bHierarchy.GetComponent<BoatController>();
        rod = rHierarchy.GetComponent<RodController>();

        playerAnimator = bHierarchy.GetChild(1).GetComponent<Animator>();
        rodAnimator = bHierarchy.GetChild(2).GetComponent<Animator>();

        playerActions = new FishingControls().Player;
        rod.SetWaterLevel(waterLevel);
        
        //uiActions = new FishingControls().UI;
        //pauseUI.SetActive(false);
        //inventoryUI.SetActive(false);
    }

    private void Start()
    {        
        playerActions.Enable();
        //playerActions.ToggleInventory.performed += (ctx) => EnableScreen(inventoryUI);
        //playerActions.TogglePause.performed += (ctx) => EnableScreen(pauseUI);
        // ---------------------------------

        isTurning = false;
        isFacingRight = true;
        isPaused = false;
    }
    
    private void EnableScreen(GameObject screen)
    {
        playerActions.Disable();
        screen.SetActive(true);
        isPaused = true;
    }

    public void DisableScreen(GameObject screen)
    {
        playerActions.Enable();
        screen.SetActive(false);
        isPaused = false;
        StartCoroutine(TimePause.UnpauseSimulation(pauseTime));
    }
    
    private void Update()
    {
        if (isPaused)
        {
            return;
        }
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
        if (isPaused)
        {
            return;
        }
        // Handle input
        var boatInput = playerActions.MoveBoat.ReadValue<float>();
        var reelInput = playerActions.ReelLine.ReadValue<float>();
        
        boat.SetBoatForce(boatInput);
        
        if (rod)
        {  
            rod.ReceiveReelInput(reelInput);
        }
    }

    public void UnpauseGame()
    {
        isPaused = false;
        pauseUI.SetActive(false);
        StartCoroutine(TimePause.UnpauseSimulation(pauseTime));
    }


}
