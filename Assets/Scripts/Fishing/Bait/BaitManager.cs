using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BaitManager : MonoBehaviour
{
    [Header("Bait References")]
    public GameObject wormBaitPrefab;
    public GameObject minnowBaitPrefab;
    public GameObject shrimpBaitPrefab;
    
    [Header("UI Elements")]
    public Button wormButton;
    public Button minnowButton;
    public Button shrimpButton;
    
    private HookController hookController;
    private PlayerInput playerInput;
    private InputAction bait1Action;
    private InputAction bait2Action;
    private InputAction bait3Action;
    
    private void Awake()
    {
        // Set up input actions
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
        
        // Create input actions
        var map = new InputActionMap("Bait");
        
        bait1Action = map.AddAction("Bait1", InputActionType.Button);
        bait1Action.AddBinding("<Keyboard>/1");
        
        bait2Action = map.AddAction("Bait2", InputActionType.Button);
        bait2Action.AddBinding("<Keyboard>/2");
        
        bait3Action = map.AddAction("Bait3", InputActionType.Button);
        bait3Action.AddBinding("<Keyboard>/3");
        
        // Enable the action map
        map.Enable();
    }
    
    private void OnEnable()
    {
        // Subscribe to input events
        bait1Action.performed += ctx => AttachBait(wormBaitPrefab);
        bait2Action.performed += ctx => AttachBait(minnowBaitPrefab);
        bait3Action.performed += ctx => AttachBait(shrimpBaitPrefab);
    }
    
    private void OnDisable()
    {
        // Unsubscribe from input events
        bait1Action.performed -= ctx => AttachBait(wormBaitPrefab);
        bait2Action.performed -= ctx => AttachBait(minnowBaitPrefab);
        bait3Action.performed -= ctx => AttachBait(shrimpBaitPrefab);
    }
    
    private void Start()
    {
        // Find the hook controller
        hookController = FindObjectOfType<HookController>();
        
        if (hookController == null)
        {
            Debug.LogError("No HookController found in scene!");
            return;
        }
        
        // Set up button listeners
        if (wormButton != null)
            wormButton.onClick.AddListener(() => AttachBait(wormBaitPrefab));
        if (minnowButton != null)
            minnowButton.onClick.AddListener(() => AttachBait(minnowBaitPrefab));
        if (shrimpButton != null)
            shrimpButton.onClick.AddListener(() => AttachBait(shrimpBaitPrefab));
            
        // Load bait prefabs if not assigned
        if (wormBaitPrefab == null)
            wormBaitPrefab = Resources.Load<GameObject>("Prefabs/Bait/WormBait");
        if (minnowBaitPrefab == null)
            minnowBaitPrefab = Resources.Load<GameObject>("Prefabs/Bait/MinnowBait");
        if (shrimpBaitPrefab == null)
            shrimpBaitPrefab = Resources.Load<GameObject>("Prefabs/Bait/ShrimpBait");
    }
    
    public void AttachBait(GameObject baitPrefab)
    {
        if (hookController != null && baitPrefab != null)
        {
            hookController.AttachBait(baitPrefab);
            Debug.Log($"Attached {baitPrefab.name} to hook");
        }
    }
} 