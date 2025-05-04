using UnityEngine;

public class FishMarketPlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private BoxCollider2D spawnArea;
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        if (spawnArea == null)
        {
            Debug.LogError("Spawn area not assigned!");
            return;
        }

        // Get the spawn position from the box collider
        Vector3 spawnPosition = spawnArea.transform.position;
        spawnPosition.z = -5f; // Set z position to -2

        Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
    }
} 