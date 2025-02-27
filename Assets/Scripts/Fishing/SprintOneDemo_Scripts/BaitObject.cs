using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class BaitObject : MonoBehaviour
{
    public Bait baitData;
    private CircleCollider2D attractionArea;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        // Set up attraction area (trigger collider)
        attractionArea = GetComponent<CircleCollider2D>();
        if (attractionArea == null)
        {
            attractionArea = gameObject.AddComponent<CircleCollider2D>();
        }
        attractionArea.isTrigger = true;
        
        // Find or create sprite renderer
        Transform spriteTransform = transform.Find("Sprite");
        if (spriteTransform == null)
        {
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }
        
        // Apply bait data
        if (baitData != null)
        {
            ApplyBaitData();
        }
    }
    
    private void ApplyBaitData()
    {
        if (attractionArea != null)
        {
            attractionArea.radius = baitData.attractionRadius;
        }
        
        if (spriteRenderer != null)
        {
            // Create a simple circle sprite for the bait
            Texture2D texture = new Texture2D(32, 32);
            Vector2 center = new Vector2(16, 16);
            float radius = 14f;
            
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance < radius)
                    {
                        texture.SetPixel(x, y, baitData.baitColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texture.Apply();
            
            Sprite baitSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            spriteRenderer.sprite = baitSprite;
            
            // Set the size
            transform.localScale = Vector3.one * baitData.size;
        }
        
        // Set layer to Bait
        gameObject.layer = LayerMask.NameToLayer("Bait");
    }
    
    public bool IsAttractedToFish(FishSize fishSize)
    {
        if (baitData == null || baitData.targetFishSizes == null)
            return true; // If no preferences set, attract all fish
            
        foreach (FishSize size in baitData.targetFishSizes)
        {
            if (size == fishSize)
                return true;
        }
        return false;
    }
    
    public float GetAttractionStrength(FishSize fishSize)
    {
        float baseStrength = baitData.attractionStrength;
        
        // Apply size-specific multiplier if this is a target fish size
        if (IsAttractedToFish(fishSize))
        {
            baseStrength *= baitData.sizeSpecificAttractionMultiplier;
        }
        
        return baseStrength;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (baitData == null) return;
        
        // Draw attraction radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, baitData.attractionRadius);
    }
} 