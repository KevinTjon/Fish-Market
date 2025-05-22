using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    private readonly float pauseTime = 0.3f;
    private FishingControls.UIActions uiActions;
    [SerializeField] private PlayerController gameManager;

    // Normal screen elements
    private Button continueButton;
    private Button quitButton;
    private Button currButton; // Current button selected

    // Confirmation screen elements
    private GameObject confirmScreen;
    private Button confirmYesButton;
    private Button confirmNoButton;
    private bool isOnConfirmScreen;

    // Start is called before the first frame update
    #region "Unity Methods"
    private void Awake()
    {
        continueButton = transform.GetChild(2).GetComponent<Button>();
        quitButton = transform.GetChild(3).GetComponent<Button>();
        
        uiActions = new FishingControls().UI;
        uiActions.Disable();
        uiActions.Navigate.performed += HandleNavigation;
        uiActions.Point.performed += HandleMouse;
        uiActions.Cancel.performed += PauseGame;

        currButton = null;

        confirmScreen = transform.GetChild(4).gameObject;
        confirmYesButton = transform.GetChild(4).GetChild(3).GetComponent<Button>();
        confirmNoButton = transform.GetChild(4).GetChild(4).GetComponent<Button>();
        confirmScreen.SetActive(false);
        isOnConfirmScreen = false;
    }

    private void OnEnable()
    {
        uiActions.Enable();
        StartCoroutine(TimePause.PauseSimulation(pauseTime));
    }

    private void OnDisable()
    {
        EventSystem.current.SetSelectedGameObject(null);
        uiActions.Disable();
        if (gameManager != null)
        {
            gameManager.DisableScreen(gameObject);
        }
        else
        {
            Debug.LogWarning("gameManager is not assigned in InventoryUI.");
        }
    }

    void OnDestroy()
    {
        uiActions.Disable();
    }
    #endregion


    #region "Input Handling"
    /// <summary>
    /// Handles navigation input for the pause menu.
    /// </summary>
    /// <param name="callContext">InputAction requirement</param>
    private void HandleNavigation(InputAction.CallbackContext callContext)
    {
        var input = callContext.ReadValue<Vector2>();
        if (isOnConfirmScreen)
        {
            NavigateToButton(input.x, confirmYesButton, confirmNoButton);
        }
        else
        {
            NavigateToButton(input.y, continueButton, quitButton);
        }
    }

    /// <summary>
    /// Navigates to the appropriate button based on the input value.
    /// </summary>
    /// <param name="input">Input float that determines button</param>
    /// <param name="b1">Button to select if input is positive</param>
    /// <param name="b2">Button to select if input is negative</param>
    private void NavigateToButton(float input, Button b1, Button b2)
    {
        switch (input)
        {
            case > 0:
                b1.Select();
                currButton = b1;
                break;
            case < 0:
                b2.Select();
                currButton = b2;
                break;
        }
    }

    /// <summary>
    /// Handles mouse input for the inventory UI.
    /// </summary>
    /// <param name="callContext">InputAction requirement</param>
    private void HandleMouse(InputAction.CallbackContext callContext)
    {
        if (currButton)
        {
            EventSystem.current.SetSelectedGameObject(null);
            currButton = null;
        }
    }

    /// <summary>
    /// Handles buttons for pausing/unpausing the game.
    /// </summary>
    /// <param name="callContext">InputAction requirement</param>
    public void PauseGame(InputAction.CallbackContext callContext)
    {
        if (callContext.action == uiActions.Cancel)
        {
            CloseUI();
        }
    }

    /// <summary>
    /// Closes the pause UI.
    /// </summary>
    public void CloseUI()
    {
        print("GamePauseUI: Cancel pressed");
        gameObject.SetActive(false);
    }
    #endregion


    #region "Confirmation Screen"
    /// <summary>
    /// Opens the confirmation screen and disables normal screen.
    /// </summary>
    public void OpenConfirmation()
    {
        EventSystem.current.SetSelectedGameObject(null);
        confirmScreen.SetActive(true);
        continueButton.interactable = false;
        quitButton.interactable = false;
    }

    /// <summary>
    /// Closes the confirmation screen and re-enables normal screen.
    /// </summary>
    public void CloseConfirmation()
    {
        EventSystem.current.SetSelectedGameObject(null);
        confirmScreen.SetActive(false);
        continueButton.interactable = true;
        quitButton.interactable = true;
    }
    
    /// <summary>
    /// Confirms the quit action and closes the game.
    /// </summary>
    public void LoadMainMenu()
    {
        uiActions.Disable();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    #endregion
}
