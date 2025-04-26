using UnityEngine;
using UnityEngine.InputSystem;

public class GamePauseUI : MonoBehaviour
{
    private readonly float pauseTime = 0.3f;
    private FishingControls.UIActions uiActions;
    [SerializeField] private PlayerController gameManager;

    // Start is called before the first frame update
    private void Awake()
    {
        uiActions = new FishingControls().UI;
        uiActions.Disable();
        uiActions.Submit.performed += PauseGame;
        uiActions.Cancel.performed += PauseGame;
    }

    private void OnEnable()
    {
        uiActions.Enable();
        StartCoroutine(TimePause.PauseSimulation(pauseTime));
    }

    private void OnDisable()
    {
        
        uiActions.Disable();
        // TODO: Change to gameManager type once implemented
        gameManager.DisableScreen(gameObject);
    }

    public void PauseGame(InputAction.CallbackContext callContext)
    {
        if (callContext.action == uiActions.Cancel)
        {
            //print("GamePauseUI: Cancel pressed");
            gameObject.SetActive(false);
        }
    }
}
