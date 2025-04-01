// using UnityEngine;
// using UnityEngine.InputSystem;

// public class GamePauseUI : MonoBehaviour
// {
//     private readonly float pauseTime = 0.3f;
//     private FishingControls.UIActions uiActions;
//     [SerializeField] private GameObject gameManager;

//     // Start is called before the first frame update
//     private void Awake()
//     {
//         uiActions = new FishingControls().UI;
//         uiActions.Disable();
//         uiActions.Submit.performed += PauseGame;
//         uiActions.Cancel.performed += PauseGame;
//     }

//     private void OnEnable()
//     {
//         uiActions.Enable();
//         //gameManager.SetActive(false);
//         StartCoroutine(TimePause.PauseSimulation(pauseTime));
//     }

//     private void OnDisable()
//     {
        
//         uiActions.Disable();
//         // TODO: Change to gameManager type once implemented
//         gameManager.GetComponent<PlayerController>().DisableScreen(gameObject);
//         //PlayerController.DisableScreen(gameObject);
//     }

//     public void PauseGame(InputAction.CallbackContext callContext)
//     {
//         if (callContext.action == uiActions.Cancel)
//         {
//             gameObject.SetActive(false);
//         }
//     }
// }
