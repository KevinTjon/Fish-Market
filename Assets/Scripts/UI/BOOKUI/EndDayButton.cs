using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndDayButton : MonoBehaviour
{
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private EndDayManager endDayManager;

    void Start()
    {
        if (nextDayButton == null)
            nextDayButton = transform.Find("NextDayButton")?.GetComponent<Button>();
        
        if (resetButton == null)
            resetButton = transform.Find("ResetButton")?.GetComponent<Button>();
            
        if (endDayManager == null)
            endDayManager = FindObjectOfType<EndDayManager>();

        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(OnNextDayClick);
            nextDayButton.interactable = true;
            nextDayButton.gameObject.SetActive(false);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClick);
            resetButton.interactable = true;
            resetButton.gameObject.SetActive(false);
        }
    }

    public void ShowButtons(bool show)
    {
        if (nextDayButton != null)
            nextDayButton.gameObject.SetActive(show);
            
        if (resetButton != null)
            resetButton.gameObject.SetActive(show);
    }

    private void OnNextDayClick()
    {
        if (endDayManager != null)
        {
            endDayManager.ProcessDay();
        }
        else
        {
            Debug.LogError("EndDayManager not found!");
        }
    }

    private void OnResetClick()
    {
        if (endDayManager != null)
        {
            endDayManager.ResetToDay1();
        }
        else
        {
            Debug.LogError("EndDayManager not found!");
        }
    }

    private void OnDestroy()
    {
        if (nextDayButton != null)
            nextDayButton.onClick.RemoveListener(OnNextDayClick);
            
        if (resetButton != null)
            resetButton.onClick.RemoveListener(OnResetClick);
    }
}