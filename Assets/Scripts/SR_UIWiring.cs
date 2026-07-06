using UnityEngine;
using UnityEngine.UI;

namespace SpeedRush
{
    public class SR_UIWiring : MonoBehaviour
    {
        private SR_PauseMenu pauseMenu;

        private void Start()
        {
            pauseMenu = GetComponent<SR_PauseMenu>();
            if (pauseMenu == null)
            {
                Debug.LogError("[UIWiring] SR_PauseMenu not found on same GameObject.");
                return;
            }

            WireButtons();
            WireSlider();
        }

        private void WireButtons()
        {
            var panel = pauseMenu.pausePanel;
            if (panel == null) return;

            WireButton(panel.transform, "ResumeButton", "ResumeText", pauseMenu.Resume);
            WireButton(panel.transform, "RestartButton", "RestartText", pauseMenu.RestartLevel);
            WireButton(panel.transform, "MainMenuButton", "MainMenuText", pauseMenu.LoadMainMenu);
        }

        private void WireButton(Transform parent, string btnName, string textName, UnityEngine.Events.UnityAction action)
        {
            var btnT = parent.Find(btnName);
            if (btnT == null) { Debug.LogWarning("[UIWiring] Button " + btnName + " not found"); return; }

            var btn = btnT.GetComponent<Button>();
            if (btn == null) { Debug.LogWarning("[UIWiring] " + btnName + " has no Button component"); return; }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);

            var txt = btnT.Find(textName)?.GetComponent<TMPro.TextMeshProUGUI>();
            if (txt == null)
            {
                txt = btnT.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }

            if (txt != null && string.IsNullOrEmpty(txt.text))
            {
                txt.text = btnName.Replace("Button", "");
                txt.fontSize = 24;
            }
        }

        private void WireSlider()
        {
            var slider = pauseMenu.musicSlider;
            if (slider == null) return;

            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(pauseMenu.OnMusicVolumeChanged);

            if (SR_BGMManager.Instance != null)
            {
                slider.value = SR_BGMManager.Instance.GetVolume();
            }
        }
    }
}
