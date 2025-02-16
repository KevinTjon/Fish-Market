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
            { "COMMON", 0.30f },
            { "UNCOMMON", 0.40f },
            { "RARE", 0.25f },
            { "LEGENDARY", 0.05f }
        };

        minFishCount = 10;
        maxFishCount = 15;
    }
}