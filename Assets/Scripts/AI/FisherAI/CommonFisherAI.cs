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
            { "Common", 0.75f },
            { "Uncommon", 0.15f },
            { "Rare", 0.05f },
            { "Epic", 0.03f },
            { "Legendary", 0.02f }
        };

        minFishCount = 15;
        maxFishCount = 20;
    }
} 