using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpeedRush
{
    public class SR_PauseMenu : MonoBehaviour
    {
        [Header("UI")]
        public GameObject pausePanel;
        public Slider musicSlider;

        [Header("Settings")]
        public string mainMenuScene = "MainMenu";

        private bool isPaused = false;
        private float savedTimeScale = 1f;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                    Resume();
                else
                    Pause();
            }
        }

        private void Awake()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (musicSlider != null && SR_BGMManager.Instance != null)
            {
                musicSlider.value = SR_BGMManager.Instance.GetVolume();
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
        }

        public void OnMusicVolumeChanged(float value)
        {
            if (SR_BGMManager.Instance != null)
            {
                SR_BGMManager.Instance.SetMixerVolume(value);
                SR_BGMManager.Instance.SetVolume(value);
            }
        }

        public void Pause()
        {
            if (isPaused) return;

            isPaused = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pausePanel != null)
                pausePanel.SetActive(true);

            Debug.Log("[PauseMenu] Game paused.");
        }

        public void Resume()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            Debug.Log("[PauseMenu] Game resumed.");
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            isPaused = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;
            SceneManager.LoadScene(mainMenuScene);
        }

        private void OnDestroy()
        {
            if (isPaused)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
