using UnityEngine;

namespace SpeedRush
{
    public class SR_Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        public bool isFinishLine = false;
        public float timeBonus = 5f;
        public Transform flipposset;

        [Header("Visual Feedback")]
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

            bool isPlayer = other.CompareTag("Player") ||
                           other.GetComponentInParent<PolyStang.CarController>() != null;

            if (!isPlayer) return;

            // Gunakan posisi dan rotasi dari FlipOffset asli dari editor (100% presisi visual)
            Vector3 spawnPos = (flipposset != null) ? flipposset.position : transform.position;
            Quaternion spawnRot = (flipposset != null) ? flipposset.rotation : transform.rotation;

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
                    SR_GameManager.Instance.UpdateLastCheckpoint(spawnPos, spawnRot, this);
                    SR_GameManager.Instance.CompleteLap();
                    ResetAllCheckpointsInScene();
                }
            }
            else
            {
                hasBeenPassed = true;
                TriggerCheckpointEffects();
                if (SR_GameManager.Instance != null)
                {
                    SR_GameManager.Instance.RegisterCheckpointPassed();
                    SR_GameManager.Instance.AddExtraTime(timeBonus);
                    SR_GameManager.Instance.UpdateLastCheckpoint(spawnPos, spawnRot, this);
                }
            }
        }

        private void TriggerCheckpointEffects()
        {
            if (activeVisual != null) activeVisual.SetActive(false);
            if (passedVisual != null) passedVisual.SetActive(true);
        }

        public void ResetCheckpoint()
        {
            hasBeenPassed = false;
            if (activeVisual != null) activeVisual.SetActive(true);
            if (passedVisual != null) passedVisual.SetActive(false);
        }

        private void ResetAllCheckpointsInScene()
        {
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
