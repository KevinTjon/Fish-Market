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
            { "COMMON", 0.35f },
            { "UNCOMMON", 0.35f },
            { "RARE", 0.20f },
            { "LEGENDARY", 0.10f }
        };

        minFishCount = 10;
        maxFishCount = 15;
    }
}