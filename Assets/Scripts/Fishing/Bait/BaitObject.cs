using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class BaitObject : MonoBehaviour
{
    public Bait baitData;
    private CircleCollider2D collider;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        // Set up collider
        collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = false;
        collider.radius = 0.2f; // Small physical collider
        
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
        {
            Debug.LogWarning($"Bait data missing: baitData={baitData != null}, targetFishSizes={baitData?.targetFishSizes != null}");
            return false; // If no preferences set, don't attract any fish
        }
            
        foreach (FishSize size in baitData.targetFishSizes)
        {
            if (size == fishSize)
                return true;
        }
        return false;
    }
} 