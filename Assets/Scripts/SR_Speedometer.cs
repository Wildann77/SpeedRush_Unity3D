using UnityEngine;
using TMPro;

namespace SpeedRush
{
    public class SR_Speedometer : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text speedText;

        [Header("Settings")]
        public float maxDisplaySpeed = 320f;

        private Rigidbody carRb;

        private void Start()
        {
            var playerCar = FindFirstObjectByType<PolyStang.CarController>();
            if (playerCar != null)
            {
                carRb = playerCar.GetComponent<Rigidbody>();
            }

            if (carRb == null)
            {
                Debug.LogWarning("[Speedometer] No player car Rigidbody found.");
            }
        }

        private void Update()
        {
            if (carRb == null || speedText == null) return;

            float speedKmh = carRb.linearVelocity.magnitude * 3.6f;
            speedText.text = Mathf.RoundToInt(speedKmh) + " km/h";

            if (speedKmh > maxDisplaySpeed * 0.85f)
            {
                speedText.color = Color.red;
            }
            else if (speedKmh > maxDisplaySpeed * 0.5f)
            {
                speedText.color = new Color(1f, 0.65f, 0f);
            }
            else
            {
                speedText.color = Color.white;
            }
        }
    }
}
