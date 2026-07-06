using UnityEngine;
using UnityEditor;
using SpeedRush;

public class CalibrateCheckpointRotation : EditorWindow
{
    [MenuItem("Tools/SpeedRush/Calibrate Checkpoint Rotation")]
    public static void ShowWindow()
    {
        GetWindow<CalibrateCheckpointRotation>("Calibrate Checkpoints");
    }

    private void OnGUI()
    {
        GUILayout.Label("Calibrate Checkpoint Respawn Rotation", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Place Car at Selected Checkpoint"))
        {
            PlaceCarAtCheckpoint();
        }

        if (GUILayout.Button("Save Car Rotation to Checkpoint"))
        {
            SaveCarRotation();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Refresh All FlipOffsets from Tangent"))
        {
            RefreshAllFromTangent();
        }
    }

    private static void PlaceCarAtCheckpoint()
    {
        GameObject car = GameObject.Find("PolyStang_1");
        if (car == null) { Debug.LogError("No PolyStang_1 found!"); return; }

        GameObject sel = Selection.activeGameObject;
        if (sel == null || sel.GetComponent<SR_Checkpoint>() == null)
        {
            Debug.LogError("Select an SR_Checkpoint in the Hierarchy first!");
            return;
        }

        Transform flip = sel.transform.Find("FlipOffset");
        if (flip == null)
        {
            GameObject offsetObj = new GameObject("FlipOffset");
            offsetObj.transform.SetParent(sel.transform);
            offsetObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            offsetObj.transform.localRotation = Quaternion.identity;
            sel.GetComponent<SR_Checkpoint>().flipposset = offsetObj.transform;
            Debug.Log("Created FlipOffset for " + sel.name);
            flip = offsetObj.transform;
        }

        Undo.RecordObject(car.transform, "Move Car to Checkpoint");
        car.transform.position = flip.position + Vector3.up * 1.5f;
        car.transform.rotation = sel.transform.rotation;

        Selection.activeGameObject = car;
        SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("Car placed at " + sel.name + ". Rotate it in Scene View, then click Save.");
    }

    private static void SaveCarRotation()
    {
        GameObject car = GameObject.Find("PolyStang_1");
        if (car == null) { Debug.LogError("No PolyStang_1 found!"); return; }

        // Find the nearest checkpoint to the car
        SR_Checkpoint[] checkpoints = Object.FindObjectsByType<SR_Checkpoint>(FindObjectsSortMode.None);
        SR_Checkpoint nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var cp in checkpoints)
        {
            if (cp.flipposset == null) continue;
            float dist = Vector3.Distance(car.transform.position, cp.flipposset.position);
            if (dist < nearestDist) { nearestDist = dist; nearest = cp; }
        }

        if (nearest == null || nearestDist > 15f)
        {
            Debug.LogError("No checkpoint found near car! Ensure car is near a checkpoint.");
            return;
        }

        Vector3 carFwd = car.transform.forward;
        carFwd.y = 0f;
        if (carFwd.sqrMagnitude < 0.01f) { Debug.LogError("Car forward too small!"); return; }

        Quaternion rot = Quaternion.LookRotation(carFwd.normalized);
        Undo.RecordObject(nearest.flipposset, "Set FlipOffset Rotation");
        nearest.flipposset.localRotation = rot;

        EditorUtility.SetDirty(nearest.flipposset.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Saved FlipOffset rotation: Y=" + rot.eulerAngles.y.ToString("F1") + " for " + nearest.name);
    }

    private static void RefreshAllFromTangent()
    {
        var sr = GameObject.Find("SR_Checkpoints");
        if (sr == null) { Debug.LogError("No SR_Checkpoints found!"); return; }

        int n = sr.transform.childCount;
        for (int i = 0; i < n; i++)
        {
            var cp = sr.transform.GetChild(i);
            var flip = cp.Find("FlipOffset");
            if (flip == null) continue;

            int pi = (i - 1 + n) % n;
            int ni = (i + 1) % n;
            var prev = sr.transform.GetChild(pi);
            var next = sr.transform.GetChild(ni);

            Vector3 d1 = (cp.position - prev.position).normalized;
            d1.y = 0f;
            Vector3 d2 = (next.position - cp.position).normalized;
            d2.y = 0f;
            Vector3 avg = (d1 + d2).normalized;

            if (avg.sqrMagnitude > 0.001f)
            {
                Quaternion rot = Quaternion.LookRotation(avg);
                Undo.RecordObject(flip, "Set FlipOffset Tangent");
                flip.localRotation = rot;
                EditorUtility.SetDirty(flip.gameObject);
                Debug.Log(cp.name + " tangent Y=" + rot.eulerAngles.y.ToString("F1"));
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("All FlipOffsets updated from tangent.");
    }

    // Public wrapper for LevelPositioningTool
    public static void RefreshAllFromTangentPublic()
    {
        RefreshAllFromTangent();
    }
}
