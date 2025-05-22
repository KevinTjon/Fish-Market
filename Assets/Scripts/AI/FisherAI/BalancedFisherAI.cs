using System.Collections.Generic;

public class BalancedFisherAI : FisherAI
{
    public BalancedFisherAI(int id = 3)
    {
        aiName = "Balanced Fisher Sam";
        aiType = AIType.BalancedFisher;
        sellerID = id;
        priceStrategy = PriceStrategy.MarketValue;
        
        rarityWeights = new Dictionary<string, float>
        {
            { "Common", 0.40f },
            { "Uncommon", 0.30f },
            { "Rare", 0.15f },
            { "Epic", 0.10f },
            { "Legendary", 0.05f }
        };

        minFishCount = 8;
        maxFishCount = 12;
    }
}