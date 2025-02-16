using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Customer
{
    public enum CUSTOMERTYPE
    {
        BUDGET,
        CASUAL,
        COLLECTOR,
        WEALTHY
    }

    public enum FISHRARITY
    {
        COMMON,
        UNCOMMON,
        RARE,
        EPIC,
        LEGENDARY
    }

    public enum SellerType
    {
        Player = 0,
        NPC = 1,
        Market = 2
    }

    [System.Serializable]
    public class ShoppingTarget
    {
        public FISHRARITY Rarity;
        public int Amount;
    }

    [System.Serializable]
    public class SellerBias
    {
        public FISHRARITY Rarity;
        public float BiasValue;
    }

    public class ShoppingListItem
    {
        public FISHRARITY Rarity { get; set; }
        public int Amount { get; set; }
    }

    // Basic properties
    public int CustomerID { get; set; }
    public CUSTOMERTYPE Type { get; set; }
    public int Budget { get; set; }
    public List<ShoppingListItem> ShoppingList { get; set; }
    public List<SellerBias> SellerPreferences { get; private set; }

    private Dictionary<(int sellerId, FISHRARITY rarity), float> biases = 
        new Dictionary<(int sellerId, FISHRARITY rarity), float>();

    // Constructor
    public Customer(CUSTOMERTYPE type, int id = 0, int? predefinedBudget = null)
    {
        CustomerID = id;
        Type = type;
        ShoppingList = new List<ShoppingListItem>();
        SellerPreferences = new List<SellerBias>();
        
        if (predefinedBudget.HasValue)
            Budget = predefinedBudget.Value;
        else
            SetCustomerProperties(type);

        // Initialize equal biases for all sellers and rarities
        InitializeSellerBiases();
    }

    private void InitializeSellerBiases()
    {
        // For each fish rarity
        foreach (FISHRARITY rarity in System.Enum.GetValues(typeof(FISHRARITY)))
        {
            // Start with equal bias (0.5) for all sellers
            SetBias(0, rarity, 0.5f);
        }
    }

    private void SetCustomerProperties(CUSTOMERTYPE type)
    {
        switch (type)
        {
            case CUSTOMERTYPE.BUDGET:
                Budget = Random.Range(20, 101);
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.COMMON, Amount = Random.Range(1, 4) });
                break;

            case CUSTOMERTYPE.CASUAL:
                Budget = Random.Range(100, 301);
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.COMMON, Amount = Random.Range(1, 3) });
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.UNCOMMON, Amount = Random.Range(0, 2) });
                break;

            case CUSTOMERTYPE.COLLECTOR:
                Budget = Random.Range(300, 801);
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.UNCOMMON, Amount = Random.Range(0, 2) });
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.RARE, Amount = Random.Range(1, 3) });
                break;

            case CUSTOMERTYPE.WEALTHY:
                Budget = Random.Range(500, 2001);
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.RARE, Amount = Random.Range(1, 3) });
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.LEGENDARY, Amount = Random.Range(0, 2) });
                ShoppingList.Add(new ShoppingListItem { Rarity = FISHRARITY.UNCOMMON, Amount = Random.Range(0, 3) });
                break;
        }
    }

    public float GetBias(int sellerId, FISHRARITY rarity)
    {
        if (biases.TryGetValue((sellerId, rarity), out float value))
        {
            return value;
        }
        return 0f;  // Default bias if none is set
    }

    public void IncreaseBias(FISHRARITY rarity, float amount = 0.1f)
    {
        var bias = SellerPreferences.FirstOrDefault(b => b.Rarity == rarity);
        
        if (bias != null)
        {
            bias.BiasValue = Mathf.Min(bias.BiasValue + amount, 1.0f);
        }
        else
        {
            SellerPreferences.Add(new SellerBias 
            { 
                Rarity = rarity, 
                BiasValue = amount 
            });
        }
    }

    public void AddToShoppingList(FISHRARITY rarity, int amount)
    {
        ShoppingList.Add(new ShoppingListItem { Rarity = rarity, Amount = amount });
    }

    public void SetBias(int sellerId, FISHRARITY rarity, float value)
    {
        biases[(sellerId, rarity)] = value;
    }

    public IEnumerable<(int sellerId, FISHRARITY rarity, float value)> GetBiases()
    {
        foreach (var kvp in biases)
        {
            yield return (kvp.Key.sellerId, kvp.Key.rarity, kvp.Value);
        }
    }

    public override string ToString()
    {
        string shoppingListStr = string.Join(", ", ShoppingList.Select(item => 
            $"{item.Amount} {item.Rarity}"));
        
        string biasStr = string.Join(", ", SellerPreferences.Select(bias =>
            $"{bias.Rarity}: {bias.BiasValue:F2}"));
        
        return $"ID={CustomerID}, Type={Type}, Budget={Budget}, " +
               $"Shopping List=[{shoppingListStr}], " +
               $"Biases=[{biasStr}]";
    }
}