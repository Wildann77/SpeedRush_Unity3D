using UnityEngine;
using TMPro;
using System.Collections;

namespace SpeedRush
{
    public class SR_StartCountdown : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text countdownText;

        [Header("Settings")]
        public float countDuration = 1f;
        public int startNumber = 3;
        public string goText = "GO!";

        private bool countdownFinished = false;
        public bool IsCountdownFinished => countdownFinished;

        private SR_GameManager gameManager;
        private PolyStang.CarController playerCar;
        private Rigidbody carRb;
        private bool wasKinematic;

        private void Start()
        {
            gameManager = SR_GameManager.Instance;
            playerCar = FindFirstObjectByType<PolyStang.CarController>();

            if (countdownText == null)
            {
                Debug.LogError("[Countdown] countdownText not assigned in Inspector.");
                FinishCountdown();
                return;
            }

            if (gameManager != null)
            {
                gameManager.SetGameActive(false);
            }

            if (playerCar != null)
            {
                carRb = playerCar.GetComponent<Rigidbody>();
                if (carRb != null)
                {
                    wasKinematic = carRb.isKinematic;
                    carRb.isKinematic = true;
                }
                playerCar.enabled = false;
            }

            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            for (int i = startNumber; i >= 1; i--)
            {
                if (countdownText != null)
                {
                    countdownText.text = i.ToString();
                    countdownText.color = Color.white;
                    countdownText.transform.localScale = Vector3.one * 1.5f;
                }

                float elapsed = 0f;
                while (elapsed < countDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / countDuration;
                    if (countdownText != null)
                    {
                        countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
                    }
                    yield return null;
                }
            }

            if (countdownText != null)
            {
                countdownText.text = goText;
                countdownText.color = Color.green;
                countdownText.transform.localScale = Vector3.one * 1.5f;
            }

            float goElapsed = 0f;
            while (goElapsed < countDuration)
            {
                goElapsed += Time.unscaledDeltaTime;
                float t = goElapsed / countDuration;
                if (countdownText != null)
                {
                    countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one * 0.8f, t);
                    countdownText.color = Color.Lerp(Color.green, new Color(0, 1, 0, 0), t);
                }
                yield return null;
            }

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }

            FinishCountdown();
            Debug.Log("[Countdown] Race started!");
        }

        private void FinishCountdown()
        {
            countdownFinished = true;

            if (playerCar != null)
            {
                playerCar.enabled = true;
            }

            if (carRb != null)
            {
                carRb.isKinematic = wasKinematic;
            }

            if (gameManager != null)
            {
                gameManager.SetGameActive(true);
            }
        }
    }
}
