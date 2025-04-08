using UnityEngine;
using UnityEngine.InputSystem;

public class BaitManager : MonoBehaviour
{
    [Header("Bait Prefabs")]
    [SerializeField] private GameObject smallBaitPrefab;
    [SerializeField] private GameObject mediumBaitPrefab;
    [SerializeField] private GameObject largeBaitPrefab;

    [Header("References")]
    [SerializeField] private RodController rodController;

    private void Update()
    {
        // Check if we have a valid hook
        if (rodController != null && rodController.hook != null)
        {
            HookController hookController = rodController.hook.GetComponent<HookController>();
            BaitSpawnArea spawnArea = rodController.hook.GetComponent<BaitSpawnArea>();
            
            if (hookController != null && spawnArea != null)
            {
                // Check if there's already bait on the hook
                Bait existingBait = hookController.GetComponentInChildren<Bait>();
                
                // If no bait exists, spawn small bait
                if (existingBait == null)
                {
                    SpawnBait(smallBaitPrefab);
                }
            }
        }
    }

    private void SpawnBait(GameObject baitPrefab)
    {
        if (rodController != null && rodController.hook != null)
        {
            HookController hookController = rodController.hook.GetComponent<HookController>();
            BaitSpawnArea spawnArea = rodController.hook.GetComponent<BaitSpawnArea>();
            
            if (hookController != null && spawnArea != null)
            {
                // Get a random position within the spawn area
                Vector2 spawnPosition = spawnArea.GetRandomSpawnPosition();
                
                // Create the bait at the random position
                GameObject baitObject = Instantiate(baitPrefab, spawnPosition, Quaternion.identity);
                
                // Parent it to the hook
                baitObject.transform.SetParent(hookController.transform);
                
                // Ensure the bait faces the same direction as the hook
                baitObject.transform.localRotation = Quaternion.identity;
            }
        }
    }
} 