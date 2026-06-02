using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpeedRush
{
    public class SR_MainMenuUI : MonoBehaviour
    {
        [Header("Scene Names")]
        public string level1Scene = "Level1";
        public string level2Scene = "Level2";

        private void Start()
        {
            // Pastikan waktu berjalan normal dan kursor bebas
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void PlayLevel1()
        {
            Debug.Log("Loading Level 1: " + level1Scene);
            SceneManager.LoadScene(level1Scene);
        }

        public void PlayLevel2()
        {
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
    }
}
