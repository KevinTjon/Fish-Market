using System.Collections.Generic;

public class RareFisherAI : FisherAI
{
    public RareFisherAI(int id = 2)
    {
        aiName = "Rare Fisher Luna";
        aiType = AIType.RareFisher;
        sellerID = id;
        priceStrategy = PriceStrategy.Conservative;
        
        rarityWeights = new Dictionary<string, float>
        {
            { "Common", 0.15f },
            { "Uncommon", 0.35f },
            { "Rare", 0.30f },
            { "Epic", 0.15f },
            { "Legendary", 0.05f }
        };

        minFishCount = 1;
        maxFishCount = 3;
    }
}