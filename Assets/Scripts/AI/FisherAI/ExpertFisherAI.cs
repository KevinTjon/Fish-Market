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
            { "Common", 0.25f },
            { "Uncommon", 0.30f },
            { "Rare", 0.25f },
            { "Epic", 0.15f },
            { "Legendary", 0.05f }
        };

        minFishCount = 10;
        maxFishCount = 15;
    }
}