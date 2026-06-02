using UnityEngine;
using UnityEditor;
using Cinemachine;
using System.Collections.Generic;

public class SetupF1Cameras : EditorWindow
{
    [MenuItem("Tools/SpeedRush/Setup F1 Cameras (HARD RESET)")]
    public static void SetupCameras()
    {
        // 1. Find the car
        GameObject car = GameObject.Find("Car");
        if (car == null) car = GameObject.Find("PolyStang_1");

        if (car == null)
        {
            Debug.LogError("Mobil TIDAK DITEMUKAN! Pastikan ada objek bernama 'Car' atau 'PolyStang_1' di Hierarchy.");
            return;
        }

        // 2. Setup Main Camera
        Camera mainCam = Camera.main;
        
        // Jika tidak ada MainCamera, cari kamera apapun
        if (mainCam == null)
        {
            Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (allCams.Length > 0) mainCam = allCams[0];
        }

        // Jika tetap tidak ada, buat baru
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            mainCam = camObj.GetComponent<Camera>();
        }

        // Pastikan Tag-nya MainCamera
        mainCam.tag = "MainCamera";
        mainCam.gameObject.SetActive(true);
        mainCam.enabled = true;

        // Tambahkan CinemachineBrain jika belum ada
        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
        }

        // Matikan kamera lain agar tidak mengganggu (kecuali Main Camera kita)
        Camera[] otherCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in otherCams)
        {
            if (cam != mainCam)
            {
                cam.enabled = false;
                Debug.Log("Menonaktifkan kamera lain: " + cam.name);
            }
        }

        // 3. Clean up old virtual cameras
        string[] oldNames = { "FreeLook Camera", "Target Follow Camera", "RearVirtualCamera", "FrontVirtualCamera" };
        foreach (var name in oldNames)
        {
            GameObject old = GameObject.Find(name);
            if (old != null) DestroyImmediate(old);
        }

        // 4. Create Target Follow Camera (UTAMA)
        // Kita gunakan Virtual Camera biasa saja dulu agar stabil
        GameObject targetCamObj = new GameObject("Target Follow Camera", typeof(CinemachineVirtualCamera));
        CinemachineVirtualCamera targetCam = targetCamObj.GetComponent<CinemachineVirtualCamera>();
        targetCam.Follow = car.transform;
        targetCam.LookAt = car.transform;
        targetCam.m_Priority = 100; // Priority sangat tinggi

        // Setup Transposer (Position)
        var transposer = targetCam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
        transposer.m_FollowOffset = new Vector3(0, 3.5f, -10f); // Agak lebih tinggi dan jauh

        // Setup Aim (LookAt)
        var composer = targetCam.AddCinemachineComponent<CinemachineComposer>();
        composer.m_TrackedObjectOffset = new Vector3(0, 1.5f, 0); // Fokus sedikit di atas mobil

        // 5. Create FreeLook Camera (CADANGAN)
        GameObject freeLookObj = new GameObject("FreeLook Camera", typeof(CinemachineFreeLook));
        CinemachineFreeLook freeLook = freeLookObj.GetComponent<CinemachineFreeLook>();
        freeLook.Follow = car.transform;
        freeLook.LookAt = car.transform;
        freeLook.m_Priority = 50; // Priority lebih rendah dari Follow Camera
        
        freeLook.m_Orbits[0] = new CinemachineFreeLook.Orbit(6f, 6f);
        freeLook.m_Orbits[1] = new CinemachineFreeLook.Orbit(3.5f, 12f);
        freeLook.m_Orbits[2] = new CinemachineFreeLook.Orbit(1f, 6f);

        // 6. Selesai
        Selection.activeGameObject = targetCamObj;
        Debug.Log("KAMERA TELAH DI-RESET TOTAL!");
        Debug.Log("Kamera sekarang menggunakan 'Target Follow Camera' sebagai utama.");
        Debug.Log("Pastikan Main Camera Anda di Inspector memiliki tag 'MainCamera'.");
    }
}
