using System.Collections;
using System.Collections.Generic;
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
        //isPaused = false;
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
            StartCoroutine(ActivatePause());
        }
        else
        {
            pauseUI.SetActive(false);
            StartCoroutine(DeactivatePause());
        }
    }

    private IEnumerator ActivatePause()
    {
        float elapsedTime = 0;
        while (elapsedTime < pauseTime)
        {
            Time.timeScale = Mathf.Lerp(1, 0, elapsedTime / pauseTime);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 0;
    }

    private IEnumerator DeactivatePause()
    {
        float elapsedTime = 0;
        while (elapsedTime < pauseTime)
        {
            Time.timeScale = Mathf.Lerp(0, 1, elapsedTime / pauseTime);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 1;
    }
}
