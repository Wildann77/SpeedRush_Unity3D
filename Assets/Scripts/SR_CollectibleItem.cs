using UnityEngine;

namespace SpeedRush
{
    public class SR_CollectibleItem : MonoBehaviour
    {
        public enum ItemType { Coin, SpeedBoost, ExtraTime }

        [Header("Collectible Settings")]
        public ItemType itemType = ItemType.Coin;
        
        [Header("Values")]
        public int scoreValue = 10;
        public float timeValue = 5f;
        
        [Header("Speed Boost Parameters")]
        public float accelerationMultiplier = 2f;
        public float speedMultiplier = 1.4f;
        public float boostDuration = 3f;

        [Header("Visual Settings")]
        public float rotateSpeed = 120f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.2f;
        public GameObject collectEffect; // Opsional: Prefab partikel saat terambil

        private Vector3 startPos;
        private bool hasBeenCollected = false;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            // Efek berputar agar item terlihat hidup
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Efek mengambang (bobbing) naik turun secara perlahan
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasBeenCollected) return;

            // Cek apakah tabrakan dengan mobil player
            PolyStang.CarController car = other.GetComponentInParent<PolyStang.CarController>();
            if (car == null) car = other.GetComponent<PolyStang.CarController>();

            if (car != null)
            {
                Collect(car);
            }
        }

        private void Collect(PolyStang.CarController car)
        {
            if (hasBeenCollected) return;
            hasBeenCollected = true;

            // Terapkan efek sesuai dengan tipe item
            switch (itemType)
            {
                case ItemType.Coin:
                    if (SR_GameManager.Instance != null)
                    {
                        SR_GameManager.Instance.AddScore(scoreValue);
                    }
                    Debug.Log("Koin berhasil diambil! +" + scoreValue + " Skor.");
                    break;

                case ItemType.ExtraTime:
                    if (SR_GameManager.Instance != null)
                    {
                        SR_GameManager.Instance.AddExtraTime(timeValue);
                    }
                    Debug.Log("Tambahan Waktu berhasil diambil! +" + timeValue + " detik.");
                    break;

                case ItemType.SpeedBoost:
                    // Panggil fungsi ApplyBoost pada CarController mobil
                    car.ApplyBoost(accelerationMultiplier, speedMultiplier, boostDuration);
                    Debug.Log("Speed Boost/Nitro diaktifkan!");
                    break;
            }

            // Spawn partikel efek jika ada
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            // Hancurkan objek koin agar tidak bisa diambil lagi
            Destroy(gameObject);
        }
    }
}
