using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class OptionsController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle ambientSoundsToggle;

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;

    [Header("Gameplay Settings")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Toggle tutorialToggle;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    private Resolution[] resolutions;

    private void Start()
    {
        // Initialize audio settings
        InitializeAudioSettings();
        
        // Initialize graphics settings
        InitializeGraphicsSettings();
        
        // Initialize gameplay settings
        InitializeGameplaySettings();

        // Set up back button
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void InitializeAudioSettings()
    {
        // Set up volume sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        if (ambientSoundsToggle != null)
        {
            ambientSoundsToggle.onValueChanged.AddListener(SetAmbientSounds);
            ambientSoundsToggle.isOn = PlayerPrefs.GetInt("AmbientSounds", 1) == 1;
        }
    }

    private void InitializeGraphicsSettings()
    {
        // Set up quality dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        // Set up resolution dropdown
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        // Set up fullscreen toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // Set up VSync toggle
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }
    }

    private void InitializeGameplaySettings()
    {
        // Set up mouse sensitivity
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        }

        // Set up invert Y toggle
        if (invertYToggle != null)
        {
            invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
            invertYToggle.onValueChanged.AddListener(SetInvertY);
        }

        // Set up tutorial toggle
        if (tutorialToggle != null)
        {
            tutorialToggle.isOn = PlayerPrefs.GetInt("ShowTutorial", 1) == 1;
            tutorialToggle.onValueChanged.AddListener(SetTutorial);
        }
    }

    // Audio Settings Methods
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void SetAmbientSounds(bool enabled)
    {
        PlayerPrefs.SetInt("AmbientSounds", enabled ? 1 : 0);
    }

    // Graphics Settings Methods
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
    }

    // Gameplay Settings Methods
    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
    }

    public void SetInvertY(bool inverted)
    {
        PlayerPrefs.SetInt("InvertY", inverted ? 1 : 0);
    }

    public void SetTutorial(bool show)
    {
        PlayerPrefs.SetInt("ShowTutorial", show ? 1 : 0);
    }

    private void OnBackClicked()
    {
        // Save all settings
        PlayerPrefs.Save();
        // Return to main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
} 