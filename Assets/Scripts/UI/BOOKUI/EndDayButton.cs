using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndDayButton : MonoBehaviour
{
    [SerializeField] private Button endDayButton;

    void Start()
    {
        if (endDayButton != null)
        {
            endDayButton = GetComponent<Button>();
           // Debug.Log("Found Button component on EndDayButton");
        }
        
        // Make sure button is interactable
        endDayButton.interactable = true;
        // Start with button hidden
        endDayButton.gameObject.SetActive(false);
        //Debug.Log("EndDayButton initialized and hidden");
    }

    public void OnEndDayClick()
    {
        //Debug.Log("WE IN HERE!");
    
        // TODO: Add scene transition logic here
        // SceneManager.LoadScene("NextDayScene");
    }
}