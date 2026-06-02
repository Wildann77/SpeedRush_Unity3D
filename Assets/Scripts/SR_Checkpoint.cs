using UnityEngine;

namespace SpeedRush
{
    public class SR_Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        public bool isFinishLine = false;
        public float timeBonus = 5f;

        [Header("Visual Feedback (Optional)")]
        public GameObject activeVisual;
        public GameObject passedVisual;

        private bool hasBeenPassed = false;

        private void Start()
        {
            ResetCheckpoint();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasBeenPassed) return;
 
            // Memastikan objek yang menabrak adalah mobil (baik badan utama maupun wheel colliders)
            bool isPlayer = other.CompareTag("Player") || 
                           other.GetComponentInParent<PolyStang.CarController>() != null || 
                           other.GetComponent<PolyStang.CarController>() != null;
 
            if (isPlayer)
            {
                // Aktifkan timer game secara otomatis jika pemain menyentuh checkpoint
                if (SR_GameManager.Instance != null && !SR_GameManager.Instance.HasStartedDriving)
                {
                    SR_GameManager.Instance.HasStartedDriving = true;
                }
                if (isFinishLine)
                {
                    if (SR_GameManager.Instance != null && SR_GameManager.Instance.CanCompleteLap())
                    {
                        hasBeenPassed = true;
                        TriggerCheckpointEffects();
                        Debug.Log("Garis Finish Dilewati dengan Sukses!");
                        SR_GameManager.Instance.UpdateLastCheckpoint(transform.position, transform.rotation);
                        SR_GameManager.Instance.CompleteLap();
                        ResetAllCheckpointsInScene();
                    }
                }
                else
                {
                    hasBeenPassed = true;
                    TriggerCheckpointEffects();
                    Debug.Log("Checkpoint Dilewati! Mendapat tambahan waktu +" + timeBonus + " detik.");
                    if (SR_GameManager.Instance != null)
                    {
                        SR_GameManager.Instance.RegisterCheckpointPassed();
                        SR_GameManager.Instance.AddExtraTime(timeBonus);
                        SR_GameManager.Instance.UpdateLastCheckpoint(transform.position, transform.rotation);
                    }
                }
            }
        }

        private void TriggerCheckpointEffects()
        {
            // Matikan visual aktif dan hidupkan visual pasif untuk feedback visual
            if (activeVisual != null) activeVisual.SetActive(false);
            if (passedVisual != null) passedVisual.SetActive(true);

            // Jika ada sound effect atau particle, bisa ditambahkan di sini
        }

        public void ResetCheckpoint()
        {
            hasBeenPassed = false;
            if (activeVisual != null) activeVisual.SetActive(true);
            if (passedVisual != null) passedVisual.SetActive(false);
        }

        private void ResetAllCheckpointsInScene()
        {
            // Cari semua checkpoint non-finish-line dan reset statusnya
            SR_Checkpoint[] checkpoints = Object.FindObjectsByType<SR_Checkpoint>(FindObjectsSortMode.None);
            foreach (var checkpoint in checkpoints)
            {
                if (!checkpoint.isFinishLine)
                {
                    checkpoint.ResetCheckpoint();
                }
            }
        }
    }
}
