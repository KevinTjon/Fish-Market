using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private readonly float pauseTime = 0.3f;
    
    private FishingControls.UIActions uiActions;
    
    // Serialize objects
    [SerializeField] private PlayerController gameManager;
    [SerializeField] private Cooler cooler;
    
    // Children references
    [SerializeField] private TMP_Text coolerList;
    
    [SerializeField] private GameObject rDetails; // Right side of the screen
    [SerializeField] private Image rFishSprite;
    [SerializeField] private TMP_Text rFishName;
    [SerializeField] private TMP_Text rFishRarity;
    [SerializeField] private TMP_Text rFishWeight;
    [SerializeField] private TMP_Text rFishDescription;

    // Start is called before the first frame update
    private void Awake()
    {
        // Input system setup
        uiActions = new FishingControls().UI;
        uiActions.Disable();
        uiActions.Submit.performed += PauseGame;
        uiActions.Cancel.performed += PauseGame;

        // Assign children to variables
        coolerList = transform.GetChild(1).GetChild(1).GetComponent<TMP_Text>();
        rDetails = transform.GetChild(2).gameObject;
        rFishSprite = rDetails.transform.GetChild(0).GetComponent<Image>();
        rFishName = Get_rFishTextComponent(1);
        rFishRarity = Get_rFishTextComponent(2);
        rFishWeight = Get_rFishTextComponent(3);
        rFishDescription = Get_rFishTextComponent(4);
        rDetails.SetActive(false); // Hide the details panel by default
    }

    // Helper function to get the text component of a variable
    private TMP_Text Get_rFishTextComponent(int childIndex)
    {
        return rDetails.transform.GetChild(childIndex)
                    .GetChild(1).GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        var entries = cooler.GetListEntries();
        var list = "";
        foreach (var entry in entries)
        {
            list += entry + "\n";
        }
        coolerList.text = list.TrimEnd('\n');
        //coolerList.GetComponent<Text>().text = list.TrimEnd('\n');

        uiActions.Enable();
        //gameManager.SetActive(false);
        StartCoroutine(TimePause.PauseSimulation(pauseTime));
    }

    private void OnDisable()
    {
        uiActions.Disable();
        // TODO: Change to gameManager type once implemented
        gameManager.DisableScreen(gameObject);
        //PlayerController.DisableScreen(gameObject);
    }

    public void PauseGame(InputAction.CallbackContext callContext)
    {
        if (callContext.action == uiActions.Cancel)
        {
            CloseUI();
        }
    }
    public void CloseUI()
    {
        print("InventoryUI: Cancel pressed");
        gameObject.SetActive(false);
    }
}
