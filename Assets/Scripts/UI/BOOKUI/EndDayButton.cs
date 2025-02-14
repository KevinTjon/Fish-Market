using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndDayButton : MonoBehaviour
{
    [SerializeField] private Button endDayButton;
    [SerializeField] private Animator bookAnimator;

    void Start()
    {
        if (endDayButton == null)
        {
            endDayButton = GetComponent<Button>();
            Debug.Log("Found Button component on EndDayButton");
        }

        if (bookAnimator == null)
        {
            bookAnimator = GameObject.Find("BookBase").GetComponent<Animator>();
            if (bookAnimator != null)
                Debug.Log("Found Book Animator on BookBase");
            else
                Debug.LogError("Could not find Book Animator! Make sure BookBase exists in scene.");
        }

        endDayButton.onClick.AddListener(OnEndDayClick);
        
        // Start with button hidden
        endDayButton.gameObject.SetActive(false);
        Debug.Log("EndDayButton initialized and hidden");
    }

    void Update()
    {
        // Only show button when book is fully closed
        bool isBookOpen = bookAnimator.GetBool("IsOpen");
        if (!isBookOpen && !endDayButton.gameObject.activeSelf)
        {
            endDayButton.gameObject.SetActive(true);
            Debug.Log("Book closed - showing End Day button");
        }
        else if (isBookOpen && endDayButton.gameObject.activeSelf)
        {
            endDayButton.gameObject.SetActive(false);
            Debug.Log("Book opened - hiding End Day button");
        }
    }

    void OnEndDayClick()
    {
        Debug.Log("End Day button clicked!");
        // TODO: Add scene transition logic here
        // SceneManager.LoadScene("NextDayScene");
    }
}