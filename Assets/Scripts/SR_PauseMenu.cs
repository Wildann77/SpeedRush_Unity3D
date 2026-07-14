using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

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

        private Button pauseButton;
        private Dictionary<GameObject, bool> prePauseStates = new Dictionary<GameObject, bool>();

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

            SetupPauseButton();
        }

        private void SetupPauseButton()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject btnObj = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(canvas.transform, false);

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0);
            btnRect.anchorMax = new Vector2(0, 0);
            btnRect.pivot = new Vector2(0, 0);
            btnRect.anchoredPosition = new Vector2(20, 20);
            btnRect.sizeDelta = new Vector2(160, 60);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);

            Text txt = txtObj.GetComponent<Text>();
            txt.text = "PAUSE";
            txt.fontSize = 36;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(Pause);
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.5f, 0.1f, 0.1f, 1f);
            btn.colors = colors;

            pauseButton = btn;
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

            if (pauseButton != null)
                pauseButton.gameObject.SetActive(false);

            prePauseStates.Clear();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                foreach (Transform child in canvas.transform)
                {
                    GameObject go = child.gameObject;
                    if (go == pausePanel) continue;
                    if (pauseButton != null && go == pauseButton.gameObject) continue;
                    prePauseStates[go] = go.activeSelf;
                    go.SetActive(false);
                }
            }

            Debug.Log("[PauseMenu] Game paused.");
        }

        public void Resume()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (pauseButton != null)
                pauseButton.gameObject.SetActive(true);

            foreach (var kvp in prePauseStates)
            {
                if (kvp.Key != null)
                    kvp.Key.SetActive(kvp.Value);
            }
            prePauseStates.Clear();

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
