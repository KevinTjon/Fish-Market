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
    private TMP_Text coolerList;
    private GameObject coolerPointer;
    private GameObject rDetails; // Right side of the screen
    private Image rFishSprite;
    private TMP_Text rFishName;
    private TMP_Text rFishRarity;
    private TMP_Text rFishWeight;
    private TMP_Text rFishDescription;

    private Vector3 pointerHomePosition;
    private int pointerIndex; // Index of the pointer in the list


    #region "Unity Methods"
    private void Awake()
    {
        // Input system setup
        uiActions = new FishingControls().UI;
        uiActions.Disable();
        uiActions.Navigate.performed += HandleNavigation;
        uiActions.Point.performed += HandleMouse;
        uiActions.Submit.performed += PauseGame;
        uiActions.Cancel.performed += PauseGame;

        // Assign children to variables
        coolerList = transform.GetChild(1).GetChild(1).GetComponent<TMP_Text>();
        coolerPointer = transform.GetChild(1).GetChild(2).gameObject;
        rDetails = transform.GetChild(2).gameObject;
        rFishSprite = rDetails.transform.GetChild(0).GetComponent<Image>();
        rFishName = Get_rFishTextComponent(1);
        rFishRarity = Get_rFishTextComponent(2);
        rFishWeight = Get_rFishTextComponent(3);
        rFishDescription = Get_rFishTextComponent(4);
        rDetails.SetActive(false);
        coolerPointer.SetActive(false);

        // Handle pointer
        pointerHomePosition = coolerPointer.transform.localPosition;
        //Debug.Log(pointerHomePosition);
        pointerIndex = -1;
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
        if (gameManager != null)
        {
            gameManager.DisableScreen(gameObject);
        }
        else
        {
            Debug.LogWarning("gameManager is not assigned in InventoryUI.");
        }
    }
    #endregion
    

    #region "Input Handling"
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
    /// Process to close the inventory UI.
    /// </summary>
    public void CloseUI()
    {
        print("InventoryUI: Cancel pressed");
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Handles mouse input for the inventory UI.
    /// </summary>
    /// <param name="callContext">InputAction requirement</param>
    public void HandleMouse(InputAction.CallbackContext callContext)
    {
        // If not hovering over text, return
        int charIndex = TMP_TextUtilities.FindIntersectingLine(
            coolerList,
            callContext.ReadValue<Vector2>(),
            null
        );
        if (charIndex == -1) return;

        // Update position
        
        if (!coolerPointer.activeSelf) ActivateDetails(charIndex);
        pointerIndex = charIndex;
        Debug.Log(charIndex);
        Debug.Log(coolerList.fontSize);
        ChangePointerPosition();
    }

    /// <summary>
    /// Handles 'Navigate' input for the inventory UI.
    /// </summary>
    /// <param name="callContext">InputAction requirement</param>
    private void HandleNavigation(InputAction.CallbackContext callContext)
    {
        // If not hovering over text, return
        if (pointerIndex == -1)
        {
            ActivateDetails(0);
            return;
        }

        // Get the direction of the navigation
        Vector2 input = callContext.ReadValue<Vector2>();
        switch (input.y)
        {
            case > 0:
                if (pointerIndex > 0) pointerIndex--;
                break;
            case < 0:
                if (pointerIndex < cooler.Count - 1) pointerIndex++;
                break;
            default:
                return;
        }

        // Update position
        ChangePointerPosition();
    }
    #endregion
    

    #region "UI Management"
    /// <summary>
    /// Activates the details panel and sets the pointer to the specified index.
    /// </summary>
    /// <param name="index">Initial fish we're pointing to</param>
    private void ActivateDetails(int index)
    {
        coolerPointer.SetActive(true);
        rDetails.SetActive(true);
        pointerIndex = index;
    }

    /// <summary>
    /// Changes the position of the pointer based on the index of the fish in the list.
    /// Also updates the details panel with the fish information.
    /// </summary>
    /// <param name="index"></param>
    private void ChangePointerPosition()
    {
        // Update pointer position
        coolerPointer.transform.localPosition = new Vector3(
            pointerHomePosition.x, 
            pointerHomePosition.y - (pointerIndex * (coolerList.fontSize+5)),
            pointerHomePosition.z
        );
        
        // Update right display
        var fish = cooler.GetFish(pointerIndex);
        rFishSprite.sprite = fish.Sprite;
        rFishName.text = fish.Name;
        rFishRarity.text = fish.Rarity.ToString();
        rFishWeight.text = fish.Weight.ToString() + " lbs.";
        rFishDescription.text = fish.Description;
    }
    #endregion
}
