using System.Collections.Generic;

public class ExpertFisherAI : FisherAI
{
    public ExpertFisherAI(int id = 4)
    {
        aiName = "Expert Fisher Kai";
        aiType = AIType.ExpertFisher;
        sellerID = id;
        priceStrategy = PriceStrategy.MarketValue;
        
        rarityWeights = new Dictionary<string, float>
        {
            { "COMMON", 0.25f },
            { "UNCOMMON", 0.30f },
            { "RARE", 0.25f },
            { "EPIC", 0.15f },
            { "LEGENDARY", 0.05f }
        };

        minFishCount = 10;
        maxFishCount = 15;
    }
}