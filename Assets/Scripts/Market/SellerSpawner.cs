using UnityEngine;

public class SimpleSellerSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SellerInfo
    {
        public BoxCollider2D spawnArea;
        public Sprite sellerSprite;
        public float scale = 1f;
        public Color tint = Color.white;
    }

    [SerializeField] private SellerInfo[] sellers;
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAllSellers();
        }
    }

    public void SpawnAllSellers()
    {
        foreach (var seller in sellers)
        {
            SpawnSeller(seller);
        }
    }

    private void SpawnSeller(SellerInfo info)
    {
        if (info.spawnArea == null)
        {
            Debug.LogWarning("Cannot spawn seller: No spawn area assigned");
            return;
        }

        if (info.sellerSprite == null)
        {
            Debug.LogWarning("Cannot spawn seller: No sprite assigned");
            return;
        }

        // Get the collider's position but set z to -10
        Vector3 spawnPosition = info.spawnArea.transform.position;
        spawnPosition.z = -1f;

        // Create a new GameObject for the seller
        GameObject sellerObj = new GameObject("Seller");
        sellerObj.transform.position = spawnPosition;
        
        // Add a sprite renderer and set the sprite
        SpriteRenderer renderer = sellerObj.AddComponent<SpriteRenderer>();
        renderer.sprite = info.sellerSprite;
        renderer.color = info.tint;
        
        // Apply scale
        sellerObj.transform.localScale = new Vector3(info.scale, info.scale, 1f);
        
        Debug.Log($"Spawned seller at {spawnPosition}");
    }
} 