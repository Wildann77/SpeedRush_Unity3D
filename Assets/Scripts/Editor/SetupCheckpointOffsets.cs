using UnityEngine;
using UnityEditor;
using SpeedRush;

public class SetupCheckpointOffsets : EditorWindow
{
    [MenuItem("Tools/SpeedRush/Setup Checkpoint Offsets")]
    public static void SetupOffsets()
    {
        // Temukan semua checkpoint di scene aktif
        SR_Checkpoint[] checkpoints = Object.FindObjectsByType<SR_Checkpoint>(FindObjectsSortMode.None);
        
        if (checkpoints.Length == 0)
        {
            Debug.LogWarning("Tidak ditemukan SR_Checkpoint di scene aktif!");
            return;
        }

        int createdCount = 0;
        int linkedCount = 0;

        foreach (var cp in checkpoints)
        {
            // Cek apakah cp sudah memiliki flipposset
            if (cp.flipposset == null)
            {
                // Cari apakah sudah ada child bernama "FlipOffset"
                Transform existingOffset = cp.transform.Find("FlipOffset");
                if (existingOffset == null)
                {
                    // Buat GameObject baru sebagai child
                    GameObject offsetObj = new GameObject("FlipOffset");
                    offsetObj.transform.SetParent(cp.transform);
                    
                    // Posisikan tepat di posisi checkpoint, sedikit naik (0.5m) agar mobil tidak jatuh menembus jalan
                    // Tidak ada offset forward karena checkpoint sudah ditempatkan di tengah jalan oleh desainer
                    offsetObj.transform.localPosition = new Vector3(0, 0.5f, 0f);
                    offsetObj.transform.localRotation = Quaternion.identity; // Menghadap arah forward yang sama dengan checkpoint

                    cp.flipposset = offsetObj.transform;
                    createdCount++;
                }
                else
                {
                    cp.flipposset = existingOffset;
                    linkedCount++;
                }

                // Tandai objek checkpoint agar Unity mencatat perubahan (dirty) untuk disave
                EditorUtility.SetDirty(cp);
            }
        }

        // Tandai scene sebagai dirty agar bisa disimpan
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log($"Pengaturan Checkpoint Offsets selesai! Berhasil membuat {createdCount} offset baru dan menghubungkan {linkedCount} offset yang sudah ada.");
    }
}
