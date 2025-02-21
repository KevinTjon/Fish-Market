using System.Collections.Generic;

public class CommonFisherAI : FisherAI
{
    public CommonFisherAI(int id = 1)
    {
        aiName = "Common Fisher Joe";
        aiType = AIType.CommonFisher;
        sellerID = id;
        priceStrategy = PriceStrategy.Aggressive;
        
        rarityWeights = new Dictionary<string, float>
        {
            { "COMMON", 0.75f },
            { "UNCOMMON", 0.15f },
            { "RARE", 0.05f },
            { "EPIC", 0.03f },
            { "LEGENDARY", 0.02f }
        };

        minFishCount = 15;
        maxFishCount = 20;
    }
} 