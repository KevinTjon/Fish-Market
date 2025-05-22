using UnityEngine;
using Market;
using UnityEngine.InputSystem;

public class CustomerMovementTest : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    private CustomerPurchaseManager purchaseManager;
    private InputAction testAction;

    void Awake()
    {
        // Create and configure the input action
        testAction = new InputAction("TestMovement", InputActionType.Button);
        testAction.AddBinding("<Keyboard>/t");
        testAction.performed += ctx => TestMovement();
    }

    void OnEnable()
    {
        testAction.Enable();
    }

    void OnDisable()
    {
        testAction.Disable();
    }

    void Start()
    {
        purchaseManager = FindObjectOfType<CustomerPurchaseManager>();
        if (purchaseManager == null)
        {
            Debug.LogError("CustomerPurchaseManager not found in scene!");
            return;
        }
    }

    // Call this method to test customer movement
    public void TestMovement()
    {
        if (targetTransform == null)
        {
            Debug.LogError("Target transform not set!");
            return;
        }

        purchaseManager.TestCustomerMovement(targetTransform);
    }

    void OnDestroy()
    {
        if (testAction != null)
        {
            testAction.Dispose();
        }
    }
} 