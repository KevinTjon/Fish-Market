using UnityEngine;
using UnityEngine.InputSystem;

public class UIController : MonoBehaviour
{
    private readonly float pauseTime = 0.3f;
    private FishingControls.UIActions uiActions;
    [SerializeField] private GameObject pauseUI;

    private bool isPaused;
    public bool IsPaused { get => isPaused; }

    // Start is called before the first frame update
    private void Awake()
    {
        uiActions = new FishingControls().UI;
        pauseUI.SetActive(false);
        isPaused = false;
    }

    void Start()
    {
        uiActions.PauseToggle.Enable();
    }

    private void OnEnable()
    {
        uiActions.PauseToggle.performed += PauseGame;
    }

    private void OnDisable()
    {
        uiActions.PauseToggle.Disable();
    }

    public void PauseGame(InputAction.CallbackContext callContext)
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            pauseUI.SetActive(true);
            StartCoroutine(TimePause.PauseSimulation(pauseTime));
        }
        else
        {
            pauseUI.SetActive(false);
            StartCoroutine(TimePause.UnpauseSimulation(pauseTime));
        }
    }

}
