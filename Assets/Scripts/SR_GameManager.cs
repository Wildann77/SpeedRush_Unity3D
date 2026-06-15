using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace SpeedRush
{
    public class SR_GameManager : MonoBehaviour
    {
        public static SR_GameManager Instance { get; private set; }

        [Header("Game Settings")]
        public float initialTime = 40f;
        public int totalLaps = 1;
        public string nextLevelName = "Level2";
        public string mainMenuName = "MainMenu";

        [Header("UI References (Optional)")]
        public TMP_Text timerText;
        public TMP_Text scoreText;
        public TMP_Text lapText;
        public GameObject winPanel;
        public GameObject losePanel;

        [Header("State Variables")]
        private float timeRemaining;
        private int score;
        private int currentLap = 1;
        private bool isGameActive = false;
        private int totalCheckpointsCount = 0;
        private int passedCheckpointsCount = 0;
        private bool hasStartedDriving = false;
        private Vector3 lastPassedCheckpointPos;
        private Quaternion lastPassedCheckpointRot;

        private PolyStang.CarController playerCar;
        private string timeBonusText = "";
        private Coroutine timeBonusCoroutine;

        [Header("Anti-Stuck UI")]
        private TMP_Text dynamicResetWarningText;
        private bool isWarningTextShowing = false;
        private Coroutine warningBlinkCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Pastikan kursor aktif dan terlihat saat level dimulai
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Menghubungkan tombol secara dinamis berdasarkan nama objek untuk keandalan 100%
            if (winPanel != null)
            {
                var allButtons = winPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in allButtons)
                {
                    if (btn.name.Contains("Next"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(LoadNextLevel);
                        Debug.Log("Menghubungkan " + btn.name + " ke LoadNextLevel() secara dinamis.");
                    }
                    else if (btn.name.Contains("Main"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(LoadMainMenu);
                        Debug.Log("Menghubungkan " + btn.name + " ke LoadMainMenu() secara dinamis.");
                    }
                }
            }

            if (losePanel != null)
            {
                var allButtons = losePanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in allButtons)
                {
                    if (btn.name.Contains("Restart"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(RestartLevel);
                        Debug.Log("Menghubungkan " + btn.name + " ke RestartLevel() secara dinamis.");
                    }
                    else if (btn.name.Contains("Main"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(LoadMainMenu);
                        Debug.Log("Menghubungkan " + btn.name + " ke LoadMainMenu() secara dinamis.");
                    }
                }
            }

            StartGame();
        }

        public void StartGame()
        {
            timeRemaining = initialTime;
            score = 0;
            currentLap = 1;
            isGameActive = true;
            hasStartedDriving = false; // Setel ke false agar waktu tidak langsung berkurang

            // Cari dan hitung total checkpoint normal di scene (tidak termasuk garis finish)
            SR_Checkpoint[] checkpoints = Object.FindObjectsByType<SR_Checkpoint>(FindObjectsSortMode.None);
            totalCheckpointsCount = 0;
            foreach (var cp in checkpoints)
            {
                if (!cp.isFinishLine)
                {
                    totalCheckpointsCount++;
                }
            }
            passedCheckpointsCount = 0;
            Debug.Log("Total checkpoints normal yang harus dilewati: " + totalCheckpointsCount);
 
            // Cari player car di scene
            playerCar = FindFirstObjectByType<PolyStang.CarController>();
            if (playerCar != null)
            {
                lastPassedCheckpointPos = playerCar.transform.position;
                lastPassedCheckpointRot = playerCar.transform.rotation;
            }
 
            // Sembunyikan panel UI jika diset
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
 
            Time.timeScale = 1f;
 
            UpdateUI();
        }

        public bool HasStartedDriving
        {
            get => hasStartedDriving;
            set => hasStartedDriving = value;
        }

        private void Update()
        {
            if (!isGameActive) return;

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCarToLastCheckpoint();
            }

            // Waktu HANYA berjalan jika pemain sudah mulai mengemudi (tekan tombol gas/arah atau mobil bergerak)
            if (!hasStartedDriving && playerCar != null)
            {
                bool hasInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || 
                                Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
                                Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                                Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ||
                                Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f;

                var rb = playerCar.GetComponent<Rigidbody>();
                bool isMoving = rb != null && rb.linearVelocity.magnitude > 0.2f;

                if (hasInput || isMoving)
                {
                    hasStartedDriving = true;
                    Debug.Log("Pemain mulai berkendara! Timer aktif.");
                }
            }

            if (hasStartedDriving)
            {
                if (timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;
                    if (timeRemaining <= 0)
                    {
                        timeRemaining = 0;
                        GameOver(false);
                    }
                }
            }

            UpdateUI();
        }

        public void AddExtraTime(float amount)
        {
            if (!isGameActive) return;
            timeRemaining += amount;
            // Tanpa pembatasan waktu maksimal agar pemain bisa menimbun waktu dengan terampil!
            Debug.Log("Time Added: +" + amount + " seconds. Current time: " + timeRemaining);

            if (amount > 0)
            {
                if (timeBonusCoroutine != null) StopCoroutine(timeBonusCoroutine);
                timeBonusCoroutine = StartCoroutine(TimeBonusFlashCoroutine(amount));
            }
        }

        private IEnumerator TimeBonusFlashCoroutine(float amount)
        {
            timeBonusText = string.Format(" (+{0}s)", Mathf.RoundToInt(amount));
            if (timerText != null) timerText.color = Color.green;

            yield return new WaitForSeconds(1.5f);

            timeBonusText = "";
            if (timerText != null) timerText.color = Color.white;
        }

        public void AddScore(int amount)
        {
            if (!isGameActive) return;
            score += amount;
            Debug.Log("Score Added: +" + amount + ". Total score: " + score);
        }

        public void RegisterCheckpointPassed()
        {
            if (!isGameActive) return;
            passedCheckpointsCount++;
            Debug.Log("Checkpoint dilewati: " + passedCheckpointsCount + "/" + totalCheckpointsCount);
        }

        public bool CanCompleteLap()
        {
            if (totalCheckpointsCount <= 0) return false;
            
            // Pemain harus melewati setidaknya sebagian besar checkpoint (misalnya 70%) untuk menghindari pintasan curang
            float passRatio = (float)passedCheckpointsCount / totalCheckpointsCount;
            return passRatio >= 0.7f;
        }

        public void CompleteLap()
        {
            if (!isGameActive) return;
 
            if (currentLap >= totalLaps)
            {
                GameOver(true); // Menang!
            }
            else
            {
                currentLap++;
                passedCheckpointsCount = 0; // Reset checkpoint untuk lap berikutnya
                AddExtraTime(10f); // Bonus waktu ganti lap
                Debug.Log("Lap Completed! Current Lap: " + currentLap + "/" + totalLaps);
            }
        }

        public void GameOver(bool hasWon)
        {
            isGameActive = false;
            Time.timeScale = 0.0001f; // Freeze game physics almost completely while keeping UI EventSystem active and responsive

            // Pastikan kursor aktif dan terlihat saat panel Game Over (Win / Lose) muncul
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerCar != null)
            {
                playerCar.enabled = false; // Matikan kontrol mobil
            }

            if (hasWon)
            {
                Debug.Log("Game Over: Player WON!");
                if (winPanel != null) winPanel.SetActive(true);
            }
            else
            {
                Debug.Log("Game Over: Player LOST (Time Out)!");
                if (losePanel != null) losePanel.SetActive(true);
            }
        }

        public void UpdateLastCheckpoint(Vector3 position, Quaternion rotation)
        {
            lastPassedCheckpointPos = position;
            lastPassedCheckpointRot = rotation;
            Debug.Log("Checkpoint Pemulihan terdaftar di posisi: " + position);
        }

        public void ResetCarToLastCheckpoint()
        {
            if (playerCar != null)
            {
                var rb = playerCar.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // Matikan fisika sejenak agar teleportasi mulus
                }

                // Teleportasi ke checkpoint terakhir dengan melayang sedikit agar ban tidak menembus aspal
                playerCar.transform.position = lastPassedCheckpointPos + Vector3.up * 0.8f;
                playerCar.transform.rotation = lastPassedCheckpointRot;

                if (rb != null)
                {
                    rb.isKinematic = false;
                }
                
                playerCar.ResetStuckTimer();
                Debug.Log("Mobil berhasil di-reset ke Checkpoint Terakhir!");
            }
        }

        private void UpdateUI()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                
                if (!hasStartedDriving)
                {
                    timerText.text = string.Format("Time: {0:00}:{1:00} (READY)", minutes, seconds);
                }
                else
                {
                    timerText.text = string.Format("Time: {0:00}:{1:00}{2}", minutes, seconds, timeBonusText);
                }
            }

            if (scoreText != null)
            {
                scoreText.text = "Score: " + score;
            }

            if (lapText != null)
            {
                lapText.text = "Lap: " + currentLap + "/" + totalLaps;
            }
        }

        // --- BUTTON ACTIONS ---

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadNextLevel()
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(nextLevelName))
            {
                SceneManager.LoadScene(nextLevelName);
            }
            else
            {
                Debug.LogWarning("Next Level Name is not specified!");
            }
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuName);
        }

        // --- DYNAMIC ANTI-STUCK UI SYSTEM ---
        private void SetupResetWarningUI()
        {
            if (dynamicResetWarningText != null) return;

            // Cari Canvas di scene aktif
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Tidak ditemukan Canvas untuk membuat UI Reset Warning secara dinamis.");
                return;
            }

            // Buat objek teks peringatan secara dinamis
            GameObject warningObj = new GameObject("DynamicResetWarningText", typeof(RectTransform), typeof(TextMeshProUGUI));
            warningObj.transform.SetParent(canvas.transform, false);

            dynamicResetWarningText = warningObj.GetComponent<TextMeshProUGUI>();
            dynamicResetWarningText.text = "TEKAN 'R' UNTUK RESET MOBIL";
            
            // Samakan font dengan timerText jika ada
            if (timerText != null)
            {
                dynamicResetWarningText.font = timerText.font;
            }
            
            dynamicResetWarningText.fontSize = 28;
            dynamicResetWarningText.color = Color.yellow;
            dynamicResetWarningText.alignment = TextAlignmentOptions.Center;

            // Konfigurasi letak di tengah-bawah layar secara responsif
            RectTransform rect = warningObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.25f);
            rect.anchorMax = new Vector2(0.5f, 0.25f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(500, 60);

            warningObj.SetActive(false);
        }

        public void ShowResetWarning(bool show)
        {
            if (dynamicResetWarningText == null)
            {
                SetupResetWarningUI();
            }

            if (dynamicResetWarningText != null)
            {
                if (show)
                {
                    if (!isWarningTextShowing)
                    {
                        isWarningTextShowing = true;
                        dynamicResetWarningText.gameObject.SetActive(true);
                        if (warningBlinkCoroutine != null) StopCoroutine(warningBlinkCoroutine);
                        warningBlinkCoroutine = StartCoroutine(BlinkWarningText());
                    }
                }
                else
                {
                    if (isWarningTextShowing)
                    {
                        isWarningTextShowing = false;
                        if (warningBlinkCoroutine != null) StopCoroutine(warningBlinkCoroutine);
                        dynamicResetWarningText.gameObject.SetActive(false);
                    }
                }
            }
        }

        private IEnumerator BlinkWarningText()
        {
            while (isWarningTextShowing && dynamicResetWarningText != null)
            {
                dynamicResetWarningText.enabled = !dynamicResetWarningText.enabled;
                // Menggunakan WaitForSecondsRealtime agar berkedip tetap stabil walaupun game physics melambat
                yield return new WaitForSecondsRealtime(0.4f);
            }
            if (dynamicResetWarningText != null)
            {
                dynamicResetWarningText.enabled = true;
            }
        }
    }
}
