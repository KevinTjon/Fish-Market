using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "FishingScene1"; // This matches your actual scene name

    private void Start()
    {
        // Set up button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Set title text
        if (titleText != null)
            titleText.text = "Bait & Tackle";
    }

    private void OnPlayClicked()
    {
        // Load the game scene
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            Debug.Log($"Attempting to load scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game scene name not set!");
        }
    }

    private void OnOptionsClicked()
    {
        // Load the options scene
        SceneManager.LoadScene("Options");
    }

    private void OnQuitClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
} 