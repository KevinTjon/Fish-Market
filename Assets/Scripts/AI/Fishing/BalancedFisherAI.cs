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
            { "COMMON", 0.40f },
            { "UNCOMMON", 0.35f },
            { "RARE", 0.20f },
            { "LEGENDARY", 0.05f }
        };

        minFishCount = 8;
        maxFishCount = 12;
    }
}