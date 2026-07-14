using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace SpeedRush
{
    public class SR_GameManager : MonoBehaviour
    {
        public static SR_GameManager Instance { get; private set; }

        [Header("Game Settings")]
        public float initialTime = 40f;
        public int totalLaps = 1;
        public string nextLevelName = "MainMenu";
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

        [Header("Checkpoint Info UI")]
        private TMP_Text checkpointInfoText;
        private string lastCheckpointName = "";
        private List<SR_Checkpoint> orderedCheckpoints = new List<SR_Checkpoint>();

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

            // Urutkan checkpoint secara dinamis berdasarkan letak spasial di aspal jalan
            OrderCheckpoints();

            totalCheckpointsCount = 0;
            foreach (var cp in orderedCheckpoints)
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

            // Inisialisasi awal recovery checkpoint ke Finish Line jika jaraknya dekat/di Level 2
            if (orderedCheckpoints.Count > 0)
            {
                SR_Checkpoint finishLine = orderedCheckpoints[0]; // Indeks 0 selalu FinishLine
                if (finishLine != null && finishLine.isFinishLine)
                {
                    // Deteksi posisi aspal secara dinamis menggunakan raycast fisika
                    Vector3 startPos = finishLine.transform.position;
                    if (Physics.Raycast(finishLine.transform.position, Vector3.down, out RaycastHit hit, 20f))
                    {
                        startPos = hit.point + Vector3.up * 0.5f;
                    }
                    else if (Physics.Raycast(finishLine.transform.position + Vector3.up * 10f, Vector3.down, out RaycastHit hitUp, 20f))
                    {
                        startPos = hitUp.point + Vector3.up * 0.5f;
                    }
                    else if (finishLine.flipposset != null)
                    {
                        startPos = finishLine.flipposset.position;
                    }

                    Quaternion startRot = finishLine.transform.rotation;

                    // Ambil checkpoint pertama setelah garis finish (indeks 1)
                    SR_Checkpoint checkpoint1 = (orderedCheckpoints.Count > 1) ? orderedCheckpoints[1] : null;

                    if (checkpoint1 != null)
                    {
                        Vector3 dirToCp1 = (checkpoint1.transform.position - startPos).normalized;
                        dirToCp1.y = 0f;
                        if (dirToCp1.sqrMagnitude > 0.01f)
                        {
                            startRot = Quaternion.LookRotation(dirToCp1);
                        }
                    }

                    // Terapkan ke recovery point jika di Level 2 atau jarak mobil dekat start
                    bool isLevel2 = SceneManager.GetActiveScene().name == "Level2";
                    if (playerCar != null && (isLevel2 || Vector3.Distance(playerCar.transform.position, startPos) < 25f))
                    {
                        lastPassedCheckpointPos = startPos;
                        lastPassedCheckpointRot = startRot;
                        Debug.Log("[GameManager] Recovery point awal diatur di Finish Line: " + lastPassedCheckpointPos + " facing " + lastPassedCheckpointRot.eulerAngles);
                    }
                }
            }
 
            // Sembunyikan panel UI jika diset
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
 
            Time.timeScale = 1f;
 
            SetupTimerPanel();
 
            UpdateUI();
        }

        private void OrderCheckpoints()
        {
            orderedCheckpoints.Clear();
            
            // Cari seluruh checkpoint di scene
            SR_Checkpoint[] allCps = Object.FindObjectsByType<SR_Checkpoint>(FindObjectsSortMode.None);
            if (allCps.Length == 0) return;

            // Pisahkan FinishLine dan checkpoint biasa
            SR_Checkpoint finishLine = null;
            List<SR_Checkpoint> normalCheckpoints = new List<SR_Checkpoint>();
            foreach (var cp in allCps)
            {
                if (cp.isFinishLine)
                    finishLine = cp;
                else
                    normalCheckpoints.Add(cp);
            }

            // Urutkan checkpoint biasa berdasarkan angka di namanya (Checkpoint_1, Checkpoint_2, dll.)
            normalCheckpoints.Sort((a, b) => {
                string numA = System.Text.RegularExpressions.Regex.Match(a.name, @"\d+").Value;
                string numB = System.Text.RegularExpressions.Regex.Match(b.name, @"\d+").Value;
                if (int.TryParse(numA, out int valA) && int.TryParse(numB, out int valB))
                {
                    return valA.CompareTo(valB);
                }
                return string.Compare(a.name, b.name);
            });

            // Finish line selalu di indeks 0
            if (finishLine != null)
            {
                orderedCheckpoints.Add(finishLine);
            }
            orderedCheckpoints.AddRange(normalCheckpoints);

            Debug.Log("[GameManager] Checkpoints berhasil diurutkan berdasarkan nama.");
            for (int i = 0; i < orderedCheckpoints.Count; i++)
            {
                Debug.Log($"Index {i}: {orderedCheckpoints[i].name}");
            }
        }

        public SR_Checkpoint GetNextCheckpoint(SR_Checkpoint current)
        {
            if (orderedCheckpoints == null || orderedCheckpoints.Count == 0) return null;
            int index = orderedCheckpoints.IndexOf(current);
            if (index == -1) return null;
            
            int nextIndex = (index + 1) % orderedCheckpoints.Count;
            return orderedCheckpoints[nextIndex];
        }


        public bool HasStartedDriving
        {
            get => hasStartedDriving;
            set => hasStartedDriving = value;
        }

        public bool IsGameActive => isGameActive;

        public void SetGameActive(bool active)
        {
            isGameActive = active;
            if (active)
            {
                Time.timeScale = 1f;
            }
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
            Time.timeScale = 0.0001f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerCar != null)
            {
                playerCar.enabled = false;
            }

            // Sembunyiin SEMUA HUD, cuma win/lose panel yg tampil
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                foreach (Transform child in canvas.transform)
                {
                    GameObject go = child.gameObject;
                    if (go == winPanel || go == losePanel) continue;
                    go.SetActive(false);
                }
            }

            if (hasWon)
            {
                Debug.Log("Game Over: Player WON!");
                PlayerPrefs.SetInt("Level2Unlocked", 1);
                PlayerPrefs.Save();
                if (winPanel != null)
                {
                    winPanel.SetActive(true);

                    Transform winTextTf = winPanel.transform.Find("WinText");
                    if (winTextTf != null)
                    {
                        TMP_Text winTmp = winTextTf.GetComponent<TMP_Text>();
                        if (winTmp != null) winTmp.text = "GAME COMPLETED";
                    }

                    bool hasNextLevel = !string.IsNullOrEmpty(nextLevelName) && nextLevelName != "MainMenu";
                    Transform nextBtnTf = winPanel.transform.Find("NextLevelButton");
                    if (nextBtnTf != null) nextBtnTf.gameObject.SetActive(hasNextLevel);
                }
            }
            else
            {
                Debug.Log("Game Over: Player LOST (Time Out)!");
                if (losePanel != null) losePanel.SetActive(true);
            }
        }

        public void UpdateLastCheckpoint(Vector3 position, Quaternion rotation, SR_Checkpoint cp = null)
        {
            lastPassedCheckpointPos = position;
            lastPassedCheckpointRot = rotation;
            if (cp != null)
            {
                if (cp.isFinishLine)
                {
                    lastCheckpointName = "Finish Line";
                }
                else
                {
                    int index = orderedCheckpoints.IndexOf(cp);
                    lastCheckpointName = (index != -1) ? index.ToString() : cp.name;
                }

                if (checkpointInfoText != null)
                {
                    checkpointInfoText.text = "CP: " + lastCheckpointName;
                    checkpointInfoText.gameObject.SetActive(true);
                }
            }
            Debug.Log("Checkpoint Pemulihan terdaftar di posisi: " + position);
        }

        public void ResetCarToLastCheckpoint()
        {
            if (playerCar != null)
            {
                StartCoroutine(ResetCarCoroutine());
            }
        }

        private IEnumerator ResetCarCoroutine()
        {
            var rb = playerCar.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;

                // Kunci rotasi agar WheelCollider suspensi tidak memutar mobil saat pertama kali menyentuh jalan
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            // Matikan WheelCollider agar tidak mengacaukan posisi awal
            var wheels = playerCar.GetComponentsInChildren<WheelCollider>();
            foreach (var wheel in wheels)
            {
                wheel.enabled = false;
            }

            // Teleport: posisi di tengah jalan, tinggi cukup agar jatuh perlahan
            playerCar.transform.position = lastPassedCheckpointPos + Vector3.up * 1.5f;
            playerCar.transform.rotation = lastPassedCheckpointRot;
            Debug.Log("[RESET] rotation set to Y=" + lastPassedCheckpointRot.eulerAngles.y.ToString("F1") + " at pos=" + lastPassedCheckpointPos);

            // Tunggu beberapa frame agar mobil settle ke jalan dengan rotasi terkunci
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // Aktifkan kembali WheelCollider
            foreach (var wheel in wheels)
            {
                wheel.enabled = true;
            }

            // Aktifkan kembali fisika, pastikan kecepatan nol
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Tunggu mobil kontak jalan dulu, baru lepas kunci rotasi
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // Sekarang mobil sudah settle — lepas kunci rotasi agar bisa dikemudi normal
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
            }

            // Log rotasi aktual setelah settle
            Debug.Log("[RESET] settled rot=" + playerCar.transform.rotation.eulerAngles + " fwd=" + playerCar.transform.forward);

            playerCar.ResetStuckTimer();
            Debug.Log("Mobil berhasil di-reset ke Checkpoint Terakhir dengan aman dan stabil!");
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

            // Update checkpoint info
            if (checkpointInfoText == null)
            {
                SetupCheckpointInfoUI();
            }
            if (checkpointInfoText != null && !string.IsNullOrEmpty(lastCheckpointName))
            {
                checkpointInfoText.text = "CP: " + lastCheckpointName;
            }
        }

        private void SetupCheckpointInfoUI()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject infoObj = new GameObject("CheckpointInfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
            infoObj.transform.SetParent(canvas.transform, false);

            checkpointInfoText = infoObj.GetComponent<TextMeshProUGUI>();
            checkpointInfoText.text = "";
            checkpointInfoText.fontSize = (lapText != null) ? lapText.fontSize : 24;
            checkpointInfoText.color = Color.cyan;
            checkpointInfoText.alignment = TextAlignmentOptions.Left;

            if (timerText != null)
            {
                checkpointInfoText.font = timerText.font;
            }

            RectTransform rect = infoObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -105f);
            rect.sizeDelta = new Vector2(300, 70);

            infoObj.SetActive(false);
        }

        private void SetupTimerPanel()
        {
            if (timerText == null) return;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            RectTransform timerRect = timerText.GetComponent<RectTransform>();

            GameObject panelObj = new GameObject("TimerPanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(canvas.transform, false);

            Image panelImg = panelObj.GetComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.4f);

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = timerRect.anchorMin;
            panelRect.anchorMax = timerRect.anchorMax;
            panelRect.pivot = timerRect.pivot;
            panelRect.anchoredPosition = timerRect.anchoredPosition;
            panelRect.sizeDelta = new Vector2(500, 70);

            timerText.transform.SetParent(panelObj.transform, false);
            timerRect.anchoredPosition = Vector2.zero;
            timerRect.sizeDelta = panelRect.sizeDelta;
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
