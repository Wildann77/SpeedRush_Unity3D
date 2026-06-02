using UnityEngine;
using Cinemachine;

namespace PolyStang
{
    [DefaultExecutionOrder(-100)] // Jalan sebelum skrip lain
    public class CameraAutoSetup : MonoBehaviour
    {
        [Header("Settings")]
        public string carName = "Car";
        public string carNameAlternative = "PolyStang_1";
        public Vector3 followOffset = new Vector3(0, 3.5f, -10f);

        private void Awake()
        {
            Setup();
        }

        [ContextMenu("Force Setup Now")]
        public void Setup()
        {
            // 1. Cari Mobil
            GameObject car = GameObject.Find(carName);
            if (car == null) car = GameObject.Find(carNameAlternative);

            if (car == null)
            {
                // Jika masih tidak ketemu, cari objek dengan Rigidbody (biasanya mobil)
                Rigidbody rb = Object.FindFirstObjectByType<Rigidbody>();
                if (rb != null) car = rb.gameObject;
            }

            if (car == null)
            {
                Debug.LogError("[CameraAutoSetup] Mobil tidak ditemukan! Berikan nama yang benar di Inspector.");
                return;
            }

            // 2. Cari atau Buat Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();
            
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                mainCam = camObj.GetComponent<Camera>();
            }

            mainCam.tag = "MainCamera";
            
            // 3. Pastikan ada CinemachineBrain
            CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null) brain = mainCam.gameObject.AddComponent<CinemachineBrain>();

            // 4. Setup Virtual Camera
            // Kita cari apakah sudah ada, kalau belum buat baru
            CinemachineVirtualCamera vcam = Object.FindFirstObjectByType<CinemachineVirtualCamera>();
            if (vcam == null)
            {
                GameObject vcamObj = new GameObject("Runtime Follow Camera", typeof(CinemachineVirtualCamera));
                vcam = vcamObj.GetComponent<CinemachineVirtualCamera>();
            }

            vcam.Follow = car.transform;
            vcam.LookAt = car.transform;
            vcam.m_Priority = 999; // Sangat tinggi agar mengalahkan yang lain

            // Setup Body (Transposer)
            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer == null) transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
            
            transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
            transposer.m_FollowOffset = followOffset;

            // Setup Aim (Composer)
            var composer = vcam.GetCinemachineComponent<CinemachineComposer>();
            if (composer == null) composer = vcam.AddCinemachineComponent<CinemachineComposer>();
            composer.m_TrackedObjectOffset = new Vector3(0, 1.5f, 0);

            Debug.Log("[CameraAutoSetup] BERHASIL! Kamera sekarang mengikuti: " + car.name);
        }
    }
}
