using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuantitySelector : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private int minQuantity = 1;
    private int maxQuantity = 1;  // Start with 1 as max until we get the real quantity
    
    private int currentQuantity = 1;

    void Start()
    {
        UpdateQuantityText();
        
        plusButton.onClick.AddListener(Increment);
        minusButton.onClick.AddListener(Decrement);
    }

    // This will be called from BigFishView with the actual fish quantity
    public void ResetWithNewMax(int availableQuantity)
    {
        Debug.Log($"Resetting quantity selector with max: {availableQuantity}");
        currentQuantity = 1;
        maxQuantity = Mathf.Max(1, availableQuantity);  // Ensure max is at least 1
        UpdateQuantityText();
        UpdateButtonStates();
    }

    void Increment()
    {
        if (currentQuantity < maxQuantity)
        {
            currentQuantity++;
            UpdateQuantityText();
            UpdateButtonStates();
        }
        Debug.Log($"Current quantity: {currentQuantity}, Max allowed: {maxQuantity}");
    }

    void Decrement()
    {
        if (currentQuantity > minQuantity)
        {
            currentQuantity--;
            UpdateQuantityText();
            UpdateButtonStates();
        }
        Debug.Log($"Current quantity: {currentQuantity}, Min allowed: {minQuantity}");
    }

    void UpdateQuantityText()
    {
        quantityText.text = currentQuantity.ToString();
    }

    void UpdateButtonStates()
    {
        plusButton.interactable = (currentQuantity < maxQuantity);
        minusButton.interactable = (currentQuantity > minQuantity);
        Debug.Log($"Button states updated - Plus: {plusButton.interactable}, Minus: {minusButton.interactable}");
    }

    public int GetQuantity()
    {
        return currentQuantity;
    }
}