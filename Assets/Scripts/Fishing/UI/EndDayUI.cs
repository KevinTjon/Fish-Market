using UnityEngine;

public class EndDayUI : MonoBehaviour
{
    [SerializeField] private Cooler cooler;
    
    private void Awake()
    {
        
        // Ensure cooler reference is set
        if (cooler == null)
        {
            cooler = FindObjectOfType<Cooler>();
            if (cooler == null)
            {
                Debug.LogWarning("No Cooler found in scene! Make sure to assign the Cooler reference.");
            }
        }
    }

    public void OnEndDayClick()
    {
        // Ensure GameSceneManager exists
        GameSceneManager.EnsureExists();
        
        // Save cooler contents to inventory
        if (cooler != null)
        {
            GameSceneManager.Instance.SaveCoolerToInventory(cooler);
            Debug.Log("Saved cooler contents to inventory");
        }
        else
        {
            Debug.LogWarning("No Cooler reference found when trying to save!");
        }
        
        // Load the market scene
        GameSceneManager.Instance.LoadMarketScene();
    }
}
