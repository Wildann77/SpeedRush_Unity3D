using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpeedRush
{
    public class SR_MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject levelSelectPanel;
        public GameObject settingsPanel;

        [Header("Scene Names")]
        public string level1Scene = "Level1";
        public string level2Scene = "Level2";

        [Header("Level Select")]
        public Button level2Button;
        public GameObject level2LockIcon;

        [Header("Settings")]
        public Slider musicSlider;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ShowMainPanel();

            UnlockLevel2IfNeeded();

            if (musicSlider != null && SR_BGMManager.Instance != null)
            {
                musicSlider.value = SR_BGMManager.Instance.GetVolume();
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
        }

        private void UnlockLevel2IfNeeded()
        {
            bool unlocked = PlayerPrefs.GetInt("Level2Unlocked", 0) == 1;
            if (level2Button != null)
                level2Button.interactable = unlocked;
            if (level2LockIcon != null)
                level2LockIcon.SetActive(!unlocked);
        }

        public void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void ShowLevelSelect()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void ShowSettings()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OnPlayClicked()
        {
            ShowLevelSelect();
        }

        public void OnSettingsClicked()
        {
            ShowSettings();
        }

        public void OnBackFromLevels()
        {
            ShowMainPanel();
        }

        public void OnBackFromSettings()
        {
            ShowMainPanel();
        }

        public void PlayLevel1()
        {
            Debug.Log("Loading Level 1: " + level1Scene);
            SceneManager.LoadScene(level1Scene);
        }

        public void PlayLevel2()
        {
            if (PlayerPrefs.GetInt("Level2Unlocked", 0) != 1)
            {
                Debug.Log("Level 2 is locked! Complete Level 1 first.");
                return;
            }
            Debug.Log("Loading Level 2: " + level2Scene);
            SceneManager.LoadScene(level2Scene);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting Game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnMusicVolumeChanged(float value)
        {
            if (SR_BGMManager.Instance != null)
            {
                SR_BGMManager.Instance.SetMixerVolume(value);
                SR_BGMManager.Instance.SetVolume(value);
            }
        }
    }
}
