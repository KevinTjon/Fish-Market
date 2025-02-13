using UnityEngine;
using UnityEngine.UI;

public class TestFishingScene : MonoBehaviour
{
    [SerializeField] private Button catchFishButton;
    [SerializeField] private Button goToMarketButton;
    [SerializeField] private Button resetInventoryButton;

    private void Start()
    {
        // Ensure GameSceneManager exists
        GameSceneManager.EnsureExists();

        // Set up button listeners
        if (catchFishButton != null)
            catchFishButton.onClick.AddListener(OnCatchFish);
        
        if (goToMarketButton != null)
            goToMarketButton.onClick.AddListener(OnGoToMarket);

        if (resetInventoryButton != null)
            resetInventoryButton.onClick.AddListener(OnResetInventory);
    }

    private void OnCatchFish()
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.SimulateFishCatch();
    }

    private void OnGoToMarket()
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadMarketScene();
    }

    private void OnResetInventory()
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.ResetInventory();
    }
}