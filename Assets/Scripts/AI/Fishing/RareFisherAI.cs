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
            { "COMMON", 0.15f },
            { "UNCOMMON", 0.35f },
            { "RARE", 0.30f },
            { "EPIC", 0.15f },
            { "LEGENDARY", 0.05f }
        };

        minFishCount = 1;
        maxFishCount = 3;
    }
}