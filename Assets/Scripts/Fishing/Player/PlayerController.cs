using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // BOAT FEATURES
    private FishingControls.PlayerActions playerActions;
    // References to other Components/GameObjects
    private BoatController boat;
    private Animator playerAnimator;

    // UI Variables
    private readonly float pauseTime = 0.3f;
    //private FishingControls.UIActions uiActions;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject inventoryUI;

    // Player flags
    private bool isPaused;
    
    // Public get variable for isPaused for UI screens
    public bool IsPaused { get => isPaused; }

    private void Awake()
    {
        var currTransform = gameObject.transform;
        var bHierarchy = currTransform.GetChild(0);
        var rHierarchy = currTransform.GetChild(1);
        boat = bHierarchy.GetComponent<BoatController>();

        playerAnimator = bHierarchy.GetChild(1).GetComponent<Animator>();

        playerActions = new FishingControls().Player;

        
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
    

    private void FixedUpdate()
    {
        if (isPaused)
        {
            return;
        }
    }

    public void UnpauseGame()
    {
        isPaused = false;
        pauseUI.SetActive(false);
        StartCoroutine(TimePause.UnpauseSimulation(pauseTime));
    }
}
