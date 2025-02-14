using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuantitySelector : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private int maxQuantity = 99;
    
    private int currentQuantity = 1;

    void Start()
    {
        UpdateQuantityText();
        
        plusButton.onClick.AddListener(Increment);
        minusButton.onClick.AddListener(Decrement);
    }

    void Increment()
    {
        if (currentQuantity < maxQuantity)
        {
            currentQuantity++;
            UpdateQuantityText();
        }
    }

    void Decrement()
    {
        if (currentQuantity > minQuantity)
        {
            currentQuantity--;
            UpdateQuantityText();
        }
    }

    void UpdateQuantityText()
    {
        quantityText.text = currentQuantity.ToString();
    }

    public int GetQuantity()
    {
        return currentQuantity;
    }
}