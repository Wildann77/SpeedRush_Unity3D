using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using SpeedRush;
using System.Collections.Generic;

public static class SetupOvalLevel
{
    [MenuItem("SpeedRush/Setup Oval Level 2")]
    public static void Run()
    {
        string destScene = "Assets/Scenes/Level2.unity";
        string sourceScene = "Assets/Environmental Race Track Pack/Scenes/OvalDisplayScene.unity";
        string gameplayScene = "Assets/Scenes/Level1.unity";

        Debug.Log("[SETUP] Memulai penyusunan Oval Level 2...");

        // 1. Salin scene display menjadi Level 2
        if (File.Exists(destScene))
        {
            File.Delete(destScene);
        }
        File.Copy(sourceScene, destScene);
        AssetDatabase.Refresh();

        // 2. Buka scene tujuan
        var destSceneObj = EditorSceneManager.OpenScene(destScene, OpenSceneMode.Single);

        // 3. Buka Level 1 secara aditif untuk mengambil komponen gameplay
        var gameplaySceneObj = EditorSceneManager.OpenScene(gameplayScene, OpenSceneMode.Additive);

        // 4. Cari objek gameplay secara andal dari root Level 1
        GameObject gameplayCps = null;
        GameObject playerCar = null;
        GameObject canvas = null;
        GameObject eventSystem = null;
        GameObject collectibles = null;

        foreach (var rootGo in gameplaySceneObj.GetRootGameObjects())
        {
            if (rootGo.name == "SR_Checkpoints") gameplayCps = rootGo;
            if (rootGo.name == "PolyStang_1") playerCar = rootGo;
            if (rootGo.name == "Canvas" || rootGo.name == "SpeedRushCanvas") canvas = rootGo;
            if (rootGo.name == "EventSystem") eventSystem = rootGo;
            if (rootGo.name == "SR_Collectibles") collectibles = rootGo;
        }

        // Tampilkan pesan error spesifik jika ada yang null
        if (gameplayCps == null) Debug.LogError("[SETUP] Gagal menemukan 'SR_Checkpoints' di Level 1!");
        if (playerCar == null) Debug.LogError("[SETUP] Gagal menemukan 'PolyStang_1' di Level 1!");
        if (canvas == null) Debug.LogError("[SETUP] Gagal menemukan 'Canvas' atau 'SpeedRushCanvas' di Level 1!");
        if (eventSystem == null) Debug.LogError("[SETUP] Gagal menemukan 'EventSystem' di Level 1!");
        if (collectibles == null) Debug.LogError("[SETUP] Gagal menemukan 'SR_Collectibles' di Level 1!");

        if (gameplayCps == null || playerCar == null || canvas == null || eventSystem == null || collectibles == null)
        {
            EditorSceneManager.CloseScene(gameplaySceneObj, true);
            return;
        }

        // 5. Duplikasi objek ke Level 2
        GameObject newCps = GameObject.Instantiate(gameplayCps);
        GameObject newCar = GameObject.Instantiate(playerCar);
        GameObject newCanvas = GameObject.Instantiate(canvas);
        GameObject newEventSystem = GameObject.Instantiate(eventSystem);
        GameObject newCollectibles = GameObject.Instantiate(collectibles);

        newCps.name = "SR_Checkpoints";
        newCar.name = "PolyStang_1";
        newCanvas.name = canvas.name; // Tetapkan nama asli (Canvas / SpeedRushCanvas)
        newEventSystem.name = "EventSystem";
        newCollectibles.name = "SR_Collectibles";

        // Pindahkan objek ke scene Level 2
        EditorSceneManager.MoveGameObjectToScene(newCps, destSceneObj);
        EditorSceneManager.MoveGameObjectToScene(newCar, destSceneObj);
        EditorSceneManager.MoveGameObjectToScene(newCanvas, destSceneObj);
        EditorSceneManager.MoveGameObjectToScene(newEventSystem, destSceneObj);
        EditorSceneManager.MoveGameObjectToScene(newCollectibles, destSceneObj);

        // 6. Tutup Level 1 tanpa menyimpan
        EditorSceneManager.CloseScene(gameplaySceneObj, true);

        // 7. Atur posisi objek di Level 2 secara matematis presisi di atas jalan
        float centerX = -69.25f;
        float centerZ = 168f;     // Pusat Z sirkuit yang benar (offset mesh)
        float roadHeightY = 5.2f;  // Tinggi perkiraan jalan

        // Dapatkan semua checkpoint di Level 2
        var cpsList = new List<SR_Checkpoint>(newCps.GetComponentsInChildren<SR_Checkpoint>());
        
        // Cari FinishLine dan pisahkan checkpoint biasa
        SR_Checkpoint finishLine = null;
        List<SR_Checkpoint> normalCps = new List<SR_Checkpoint>();
        foreach (var cp in cpsList)
        {
            if (cp.isFinishLine) finishLine = cp;
            else normalCps.Add(cp);
        }

        // Urutkan ulang secara numerik sesuai nama aslinya
        normalCps.Sort((a, b) => {
            string numA = System.Text.RegularExpressions.Regex.Match(a.name, @"\d+").Value;
            string numB = System.Text.RegularExpressions.Regex.Match(b.name, @"\d+").Value;
            if (int.TryParse(numA, out int valA) && int.TryParse(numB, out int valB))
            {
                return valA.CompareTo(valB);
            }
            return string.Compare(a.name, b.name);
        });

        List<SR_Checkpoint> finalOrdered = new List<SR_Checkpoint>();
        if (finishLine != null) finalOrdered.Add(finishLine);
        finalOrdered.AddRange(normalCps);

        // Posisikan gerbang dan setel properti mengikuti rute jalan
        for (int i = 0; i < finalOrdered.Count; i++)
        {
            float t = (float)i / finalOrdered.Count;
            Vector3 pos = GetTrackPosition(t, centerX, centerZ, roadHeightY);
            
            // Cari tinggi jalan riil menggunakan raycast
            float actualY = GetRoadHeight(pos, roadHeightY);
            pos.y = actualY;

            // Hitung arah hadap forward lintasan secara dinamis
            Vector3 nextPos = GetTrackPosition(t + 0.005f, centerX, centerZ, roadHeightY);
            Vector3 dir = (nextPos - pos).normalized;
            dir.y = 0f;
            Quaternion rot = Quaternion.LookRotation(dir);

            var cp = finalOrdered[i];
            cp.transform.position = pos;
            cp.transform.rotation = rot;
            
            // Setel nama gerbang agar urut secara numerik
            if (cp.isFinishLine)
            {
                cp.name = "FinishLine_Checkpoint";
            }
            else
            {
                cp.name = "Checkpoint_" + i;
            }

            // Reset FlipOffset anak agar pas di tengah jalan
            Transform flip = cp.transform.Find("FlipOffset");
            if (flip != null)
            {
                flip.localPosition = new Vector3(0f, 0.5f, 0f);
                flip.localRotation = Quaternion.identity;
            }
        }

        // 8. Atur posisi mobil pemain di garis start
        if (newCar != null)
        {
            // Posisikan 15 meter di belakang FinishLine (t = 0 adalah FinishLine, gunakan t negatif kecil)
            float totalLength = 2f * 600f + 2f * Mathf.PI * 290f;
            float startT = 1f - (15f / totalLength);
            Vector3 startPos = GetTrackPosition(startT, centerX, centerZ, roadHeightY);
            float actualY = GetRoadHeight(startPos, roadHeightY);
            startPos.y = actualY + 0.15f; // Ban tepat menempel di aspal

            // Dapatkan arah hadap garis start
            Vector3 startNextPos = GetTrackPosition(startT + 0.005f, centerX, centerZ, roadHeightY);
            Vector3 startDir = (startNextPos - startPos).normalized;
            startDir.y = 0f;

            newCar.transform.position = startPos;
            newCar.transform.rotation = Quaternion.LookRotation(startDir);
        }

        // 9. Atur posisi koin & boost secara merata menggunakan rumus lintasan di tengah jalan
        if (newCollectibles != null)
        {
            var items = new List<Transform>();
            foreach (Transform child in newCollectibles.transform)
            {
                items.Add(child);
            }

            for (int i = 0; i < items.Count; i++)
            {
                // Beri offset agar item terletak di antara checkpoint (tidak menumpuk pas di gerbang)
                float t = ((float)i + 0.5f) / items.Count;
                Vector3 pos = GetTrackPosition(t, centerX, centerZ, roadHeightY);

                // Cari tinggi jalan riil
                float actualY = GetRoadHeight(pos, roadHeightY);
                pos.y = actualY + 0.8f; // Mengambang 0.8m di atas jalan

                items[i].position = pos;
                items[i].rotation = Quaternion.identity;
            }
            Debug.Log($"[SETUP] Berhasil mendistribusikan {items.Count} item kolektibel mengelilingi sirkuit.");
        }

        // Simpan scene Level 2
        EditorSceneManager.SaveScene(destSceneObj);
        Debug.Log("[SETUP] Oval Level 2 berhasil disusun dengan rapi!");
    }

    // Fungsi matematika untuk menghitung posisi di tengah jalan raya sirkuit oval (Rounded-Rectangle)
    private static Vector3 GetTrackPosition(float t, float centerX, float centerZ, float roadHeightY)
    {
        float straightLength = 600f; // Panjang jalan lurus (dari X = -369.25 ke X = 230.75)
        float radius = 290f;         // Radius belokan semi-lingkaran
        float halfStraight = straightLength / 2f; // 300m
        
        float totalLength = 2f * straightLength + 2f * Mathf.PI * radius;
        
        // Bungkus nilai t agar selalu di antara 0 dan 1
        t = t - Mathf.Floor(t);
        float dist = t * totalLength;
        
        float seg1 = halfStraight; // Belahan Selatan pertama (ke arah Timur)
        float seg2 = seg1 + Mathf.PI * radius; // Belokan Timur
        float seg3 = seg2 + straightLength; // Jalan Lurus Utara (ke arah Barat)
        float seg4 = seg3 + Mathf.PI * radius; // Belokan Barat
        
        if (dist < seg1)
        {
            // Jalan lurus Selatan (X bergerak dari 0 ke +300)
            float xRel = dist;
            return new Vector3(centerX + xRel, roadHeightY, centerZ - radius);
        }
        else if (dist < seg2)
        {
            // Belokan Timur (Sudut dari -90 derajat ke +90 derajat)
            float progress = (dist - seg1) / (Mathf.PI * radius);
            float angle = -Mathf.PI / 2f + progress * Mathf.PI;
            float xRel = halfStraight + radius * Mathf.Cos(angle);
            float zRel = radius * Mathf.Sin(angle);
            return new Vector3(centerX + xRel, roadHeightY, centerZ + zRel);
        }
        else if (dist < seg3)
        {
            // Jalan lurus Utara (X bergerak dari +300 ke -300)
            float progress = (dist - seg2) / straightLength;
            float xRel = halfStraight - progress * straightLength;
            return new Vector3(centerX + xRel, roadHeightY, centerZ + radius);
        }
        else if (dist < seg4)
        {
            // Belokan Barat (Sudut dari 90 derajat ke 270 derajat)
            float progress = (dist - seg3) / (Mathf.PI * radius);
            float angle = Mathf.PI / 2f + progress * Mathf.PI;
            float xRel = -halfStraight + radius * Mathf.Cos(angle);
            float zRel = radius * Mathf.Sin(angle);
            return new Vector3(centerX + xRel, roadHeightY, centerZ + zRel);
        }
        else
        {
            // Jalan lurus Selatan kedua (X bergerak dari -300 ke 0)
            float progress = (dist - seg4) / halfStraight;
            float xRel = -halfStraight + progress * halfStraight;
            return new Vector3(centerX + xRel, roadHeightY, centerZ - radius);
        }
    }

    // Fungsi pencari tinggi aspal jalan riil di bawah koordinat menggunakan Raycast Editor
    private static float GetRoadHeight(Vector3 pos, float defaultY)
    {
        RaycastHit hit;
        // Tembak ray dari ketinggian 100m ke bawah
        if (Physics.Raycast(new Vector3(pos.x, 100f, pos.z), Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }
        return defaultY;
    }
}
