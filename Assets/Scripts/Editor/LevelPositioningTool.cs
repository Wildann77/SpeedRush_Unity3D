using UnityEngine;
using UnityEditor;
using SpeedRush;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LevelPositioningTool : EditorWindow
{
    private Vector2 scrollPos;
    private int selectedTab = 0;
    private string[] tabs = { "Checkpoint", "Collectible", "Car", "Batch All" };

    // For checkpoint list (sorted by number)
    private List<SR_Checkpoint> allCheckpoints = new List<SR_Checkpoint>();
    private List<string> cpNames = new List<string>();
    private int selectedCpIndex = -1;

    // For collectible list
    private List<SR_CollectibleItem> allCollectibles = new List<SR_CollectibleItem>();
    private List<string> ciNames = new List<string>();
    private int selectedCiIndex = -1;

    // Car
    private GameObject car;
    private bool carFound;

    private GUIStyle headerStyle;
    private GUIStyle greenBtn;
    private GUIStyle orangeBtn;
    private GUIStyle blueBtn;

    [MenuItem("Tools/SpeedRush/Level Positioning Tool")]
    public static void ShowWindow()
    {
        var w = GetWindow<LevelPositioningTool>("Level Positioning");
        w.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        RefreshLists();
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;
        headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        greenBtn = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.green }, fontStyle = FontStyle.Bold };
        orangeBtn = new GUIStyle(GUI.skin.button) { normal = { textColor = new Color(1f, 0.6f, 0f) }, fontStyle = FontStyle.Bold };
        blueBtn = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.cyan }, fontStyle = FontStyle.Bold };
    }

    private void RefreshLists()
    {
        car = GameObject.Find("PolyStang_1");
        carFound = car != null;

        var raw = Object.FindObjectsByType<SR_Checkpoint>(FindObjectsSortMode.None);
        allCheckpoints = new List<SR_Checkpoint>();
        // FinishLine first, then sort by number
        foreach (var cp in raw) { if (cp.isFinishLine) allCheckpoints.Add(cp); }
        var rest = new List<SR_Checkpoint>();
        foreach (var cp in raw) { if (!cp.isFinishLine) rest.Add(cp); }
        rest.Sort((a, b) => {
            int na = 0, nb = 0;
            var ma = Regex.Match(a.name, @"\d+"); if (ma.Success) na = int.Parse(ma.Value);
            var mb = Regex.Match(b.name, @"\d+"); if (mb.Success) nb = int.Parse(mb.Value);
            return na.CompareTo(nb);
        });
        allCheckpoints.AddRange(rest);
        cpNames = new List<string>();
        foreach (var cp in allCheckpoints)
        {
            string rotInfo = cp.flipposset != null ? " Y=" + cp.flipposset.rotation.eulerAngles.y.ToString("F0") + "\u00b0" : " NO FLIP";
            cpNames.Add(cp.name + " | " + cp.transform.position.ToString("F1") + rotInfo);
        }

        allCollectibles = new List<SR_CollectibleItem>(Object.FindObjectsByType<SR_CollectibleItem>(FindObjectsSortMode.None));
        ciNames = new List<string>();
        foreach (var ci in allCollectibles)
        {
            string t = ci.itemType == SR_CollectibleItem.ItemType.Coin ? "Coin" :
                       ci.itemType == SR_CollectibleItem.ItemType.SpeedBoost ? "Boost" : "Time";
            ciNames.Add(ci.name + " (" + t + ") | " + ci.transform.position.ToString("F1"));
        }
    }

    private void OnGUI()
    {
        InitStyles();
        GUILayout.Label("\ud83d\udee0 Level Positioning Tool", headerStyle);
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("\u21bb Refresh List", GUILayout.Width(120))) RefreshLists();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (!carFound)
        {
            EditorGUILayout.HelpBox("PolyStang_1 tidak ditemukan di scene!", MessageType.Error);
        }

        // === SELECTED OBJECT INFO ===
        GameObject sel = Selection.activeGameObject;
        if (sel != null)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Selected: " + sel.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("World Pos: " + sel.transform.position.ToString("F3"));
            EditorGUILayout.LabelField("World Rot: " + sel.transform.rotation.eulerAngles.ToString("F1"));
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(5);

        // === TABS ===
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        GUILayout.Space(5);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (selectedTab)
        {
            case 0: DrawCheckpointTab(); break;
            case 1: DrawCollectibleTab(); break;
            case 2: DrawCarTab(); break;
            case 3: DrawBatchTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    // TAB 1: CHECKPOINT
    // ============================================================
    private void DrawCheckpointTab()
    {
        EditorGUILayout.LabelField("CHECKPOINT LIST", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Klik checkpoint di list bawah, lalu pilih aksi:", EditorStyles.miniLabel);

        GUILayout.Space(5);

        // Quick action bar
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = cpNames.Count > 0;
        if (GUILayout.Button("\u25b6 Go to CP", blueBtn, GUILayout.Height(25))) { GoToSelectedCp(); }
        if (GUILayout.Button("\u25b6 Go to FlipOffset", blueBtn, GUILayout.Height(25))) { GoToSelectedFlip(); }
        if (GUILayout.Button("\ud83d\udccd Save Pos", greenBtn, GUILayout.Height(25))) { SaveCpPosFromCar(); }
        if (GUILayout.Button("\ud83d\udd04 Save Rot", orangeBtn, GUILayout.Height(25))) { SaveCpRotFromCar(); }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = cpNames.Count > 0;
        if (GUILayout.Button("Snap Y to Road", GUILayout.Height(25))) { SnapSelectedCpY(); }
        if (GUILayout.Button("Copy Car Pos+Rot", GUILayout.Height(25))) { SaveCpPosFromCar(); SaveCpRotFromCar(); }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Scrollable list
        if (cpNames.Count == 0)
        {
            EditorGUILayout.HelpBox("Tidak ada checkpoint di scene ini", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Daftar Checkpoint (klik untuk pilih):", EditorStyles.miniBoldLabel);
            selectedCpIndex = GUILayout.SelectionGrid(selectedCpIndex, cpNames.ToArray(), 1, GUILayout.Height(cpNames.Count * 22));
        }
    }

    // ============================================================
    // TAB 2: COLLECTIBLE
    // ============================================================
    private void DrawCollectibleTab()
    {
        EditorGUILayout.LabelField("COLLECTIBLE LIST", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Klik item di list, lalu pilih aksi:", EditorStyles.miniLabel);

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = ciNames.Count > 0;
        if (GUILayout.Button("\u25b6 Go to Item", blueBtn, GUILayout.Height(25))) { GoToSelectedCi(); }
        if (GUILayout.Button("\ud83d\udccd Save Pos", greenBtn, GUILayout.Height(25))) { SaveCiPosFromCar(); }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = ciNames.Count > 0;
        if (GUILayout.Button("Snap Y to Road", GUILayout.Height(25))) { SnapSelectedCiY(); }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (ciNames.Count == 0)
        {
            EditorGUILayout.HelpBox("Tidak ada collectible di scene ini", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Daftar Collectible (klik untuk pilih):", EditorStyles.miniBoldLabel);
            selectedCiIndex = GUILayout.SelectionGrid(selectedCiIndex, ciNames.ToArray(), 1, GUILayout.Height(ciNames.Count * 22));
        }
    }

    // ============================================================
    // TAB 3: CAR INFO
    // ============================================================
    private void DrawCarTab()
    {
        EditorGUILayout.LabelField("CAR POSITION", EditorStyles.boldLabel);

        if (!carFound)
        {
            EditorGUILayout.HelpBox("PolyStang_1 tidak ada di scene", MessageType.Error);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Car: PolyStang_1");
        EditorGUILayout.LabelField("Position: " + car.transform.position.ToString("F3"));
        EditorGUILayout.LabelField("Rotation: " + car.transform.rotation.eulerAngles.ToString("F1"));
        EditorGUILayout.LabelField("Forward: " + car.transform.forward.ToString("F2"));
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        if (GUILayout.Button("\ud83d\udd34 Focus Scene View to Car", GUILayout.Height(30)))
        {
            FocusCarInSceneView();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Snap Car Y to Road Below", GUILayout.Height(30)))
        {
            SnapCarToRoad();
        }

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Start Position", EditorStyles.boldLabel);

        if (GUILayout.Button("Set Start: Behind FinishLine (15m)", GUILayout.Height(30)))
        {
            PlaceCarBehindFinishLine(15f);
        }

        if (GUILayout.Button("Set Start: Behind FinishLine (30m)", GUILayout.Height(30)))
        {
            PlaceCarBehindFinishLine(30f);
        }

        if (GUILayout.Button("Save Current Pos as Start", greenBtn, GUILayout.Height(30)))
        {
            SaveCurrentCarAsStart();
        }

        GUILayout.Space(5);

        EditorGUILayout.HelpBox("Cara pakai:\n1. Klik 'Set Start' untuk auto-place mobil di belakang FinishLine\n2. Atau drive mobil sendiri ke posisi start\n3. Klik 'Save Current Pos as Start' untuk konfirmasi\n\nPosisi mobil di scene = posisi start saat game mulai.", MessageType.Info);
    }

    // ============================================================
    // TAB 4: BATCH
    // ============================================================
    private void DrawBatchTab()
    {
        EditorGUILayout.LabelField("BATCH OPERATIONS", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Fix semua sekaligus:", EditorStyles.miniLabel);

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Checkpoints", EditorStyles.boldLabel);
        if (GUILayout.Button("Snap ALL Checkpoint Y to Road", GUILayout.Height(30)))
        {
            SnapAllCheckpointsToRoad();
        }
        if (GUILayout.Button("Refresh ALL FlipOffset Rotation from Tangent", GUILayout.Height(30)))
        {
            RefreshAllFlipOffsetsFromTangent();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Collectibles", EditorStyles.boldLabel);
        if (GUILayout.Button("Snap ALL Collectible Y to Road", GUILayout.Height(30)))
        {
            SnapAllCollectiblesToRoad();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Info:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Checkpoint di scene: " + allCheckpoints.Count);
        EditorGUILayout.LabelField("Collectible di scene: " + allCollectibles.Count);
        EditorGUILayout.LabelField("Car ditemukan: " + (carFound ? "Ya" : "Tidak"));
        EditorGUILayout.EndVertical();
    }

    // ============================================================
    // CHECKPOINT ACTIONS
    // ============================================================
    private SR_Checkpoint GetSelectedCp()
    {
        if (selectedCpIndex < 0 || selectedCpIndex >= allCheckpoints.Count) return null;
        return allCheckpoints[selectedCpIndex];
    }

    private void FocusCarInSceneView()
    {
        if (carFound)
        {
            Selection.activeGameObject = car;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }
        }
    }

    private void GoToSelectedCp()
    {
        var cp = GetSelectedCp();
        if (cp == null || !carFound) return;
        Undo.RecordObject(car.transform, "Move Car");
        Vector3 dir = GetTangentDir(cp);
        car.transform.position = cp.transform.position + Vector3.up * 1.5f;
        if (dir.sqrMagnitude > 0.01f)
            car.transform.rotation = Quaternion.LookRotation(dir);
        FocusCarInSceneView();
        Debug.Log("Mobil pindah ke " + cp.name);
    }

    private void GoToSelectedFlip()
    {
        var cp = GetSelectedCp();
        if (cp == null || cp.flipposset == null || !carFound) return;
        Undo.RecordObject(car.transform, "Move Car");
        car.transform.position = cp.flipposset.position + Vector3.up * 1.5f;
        car.transform.rotation = cp.flipposset.rotation;
        FocusCarInSceneView();
        Debug.Log("Mobil pindah ke FlipOffset " + cp.name);
    }

    private void SaveCpPosFromCar()
    {
        var cp = GetSelectedCp();
        if (cp == null || !carFound) return;
        Undo.RecordObject(cp.transform, "Move CP");
        cp.transform.position = new Vector3(car.transform.position.x, cp.transform.position.y, car.transform.position.z);
        if (cp.flipposset != null)
        {
            Undo.RecordObject(cp.flipposset, "Fix FlipOffset");
            cp.flipposset.localPosition = new Vector3(0, 0.5f, 0);
        }
        EditorUtility.SetDirty(cp.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log("Checkpoint " + cp.name + " XZ = posisi mobil");
    }

    private void SaveCpRotFromCar()
    {
        var cp = GetSelectedCp();
        if (cp == null || cp.flipposset == null || !carFound) return;
        Vector3 fwd = car.transform.forward;
        fwd.y = 0;
        if (fwd.sqrMagnitude < 0.01f) return;
        Undo.RecordObject(cp.flipposset, "Set Rot");
        // Compensate parent rotation: localRot = Inverse(parentRot) * targetWorldRot
        Quaternion targetWorldRot = Quaternion.LookRotation(fwd.normalized);
        Quaternion parentRot = cp.transform.rotation;
        cp.flipposset.localRotation = Quaternion.Inverse(parentRot) * targetWorldRot;
        EditorUtility.SetDirty(cp.flipposset.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log("FlipOffset " + cp.name + " localRot Y=" + cp.flipposset.localRotation.eulerAngles.y.ToString("F1") + " → worldRot Y=" + cp.flipposset.rotation.eulerAngles.y.ToString("F1"));
    }

    private void SnapSelectedCpY()
    {
        var cp = GetSelectedCp();
        if (cp == null) return;
        Undo.RecordObject(cp.transform, "Snap Y");
        Vector3 pos = cp.transform.position;
        float y = RaycastRoadY(pos);
        if (y < -100f) { Debug.LogWarning("Tidak ada road di bawah " + cp.name); return; }
        cp.transform.position = new Vector3(pos.x, y, pos.z);
        if (cp.flipposset != null)
        {
            Undo.RecordObject(cp.flipposset, "Fix FlipOffset");
            cp.flipposset.localPosition = new Vector3(0, 0.5f, 0);
        }
        EditorUtility.SetDirty(cp.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log(cp.name + " Y=" + y.ToString("F2"));
    }

    // ============================================================
    // COLLECTIBLE ACTIONS
    // ============================================================
    private SR_CollectibleItem GetSelectedCi()
    {
        if (selectedCiIndex < 0 || selectedCiIndex >= allCollectibles.Count) return null;
        return allCollectibles[selectedCiIndex];
    }

    private void GoToSelectedCi()
    {
        var ci = GetSelectedCi();
        if (ci == null || !carFound) return;
        Undo.RecordObject(car.transform, "Move Car");
        car.transform.position = ci.transform.position + Vector3.up * 0.5f;
        FocusCarInSceneView();
        Debug.Log("Mobil pindah ke " + ci.name);
    }

    private void SaveCiPosFromCar()
    {
        var ci = GetSelectedCi();
        if (ci == null || !carFound) return;
        Undo.RecordObject(ci.transform, "Move Item");
        ci.transform.position = car.transform.position;
        EditorUtility.SetDirty(ci.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log(ci.name + " pindah ke posisi mobil");
    }

    private void SnapSelectedCiY()
    {
        var ci = GetSelectedCi();
        if (ci == null) return;
        Undo.RecordObject(ci.transform, "Snap Y");
        Vector3 pos = ci.transform.position;
        float y = RaycastRoadY(pos);
        if (y < -100f) { Debug.LogWarning("Tidak ada road di bawah " + ci.name); return; }
        ci.transform.position = new Vector3(pos.x, y + 0.4f, pos.z);
        EditorUtility.SetDirty(ci.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log(ci.name + " Y=" + (y + 0.4f).ToString("F2"));
    }

    // ============================================================
    // CAR ACTIONS
    // ============================================================
    private void SnapCarToRoad()
    {
        if (!carFound) return;
        Undo.RecordObject(car.transform, "Snap Car");
        Vector3 pos = car.transform.position;
        float y = RaycastRoadY(pos);
        if (y < -100f) { Debug.LogWarning("Tidak ada road di bawah mobil"); return; }
        car.transform.position = new Vector3(pos.x, y + 0.5f, pos.z);
        Debug.Log("Mobil Y=" + (y + 0.5f).ToString("F2"));
    }

    private void PlaceCarBehindFinishLine(float distanceBehind)
    {
        if (!carFound) return;

        SR_Checkpoint finishLine = null;
        foreach (var cp in allCheckpoints)
        {
            if (cp.isFinishLine) { finishLine = cp; break; }
        }
        if (finishLine == null) { Debug.LogWarning("FinishLine tidak ditemukan"); return; }

        Vector3 flPos = finishLine.transform.position;

        // Find tangent direction at finish line (reverse = direction car should face = towards FinishLine)
        Vector3 tangent = GetTangentDir(finishLine);
        if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.forward;

        // Place car BEHIND finish line (opposite of tangent = where car comes from)
        Vector3 startPos = flPos - tangent * distanceBehind;
        float roadY = RaycastRoadY(startPos);
        if (roadY < -100f) roadY = flPos.y;
        startPos.y = roadY + 0.5f;

        Quaternion startRot = Quaternion.LookRotation(tangent); // Face forward (toward finish line)

        Undo.RecordObject(car.transform, "Place Car at Start");
        car.transform.position = startPos;
        car.transform.rotation = startRot;

        FocusCarInSceneView();

        Debug.Log("Mobil di belakang FinishLine: " + startPos.ToString("F2") + " facing Y=" + startRot.eulerAngles.y.ToString("F0") + "°");
    }

    private void SaveCurrentCarAsStart()
    {
        if (!carFound) return;
        Undo.RecordObject(car.transform, "Save Start Position");
        Debug.Log("Start position disimpan: " + car.transform.position.ToString("F2") + " Rot Y=" + car.transform.rotation.eulerAngles.y.ToString("F0") + "°");
        Debug.Log("Pindah ke scene lain atau Play untuk test.");
    }

    // ============================================================
    // BATCH ACTIONS
    // ============================================================
    private void SnapAllCheckpointsToRoad()
    {
        foreach (var cp in allCheckpoints)
        {
            Undo.RecordObject(cp.transform, "Snap Y");
            Vector3 pos = cp.transform.position;
            float y = RaycastRoadY(pos);
            if (y > -100f)
            {
                cp.transform.position = new Vector3(pos.x, y, pos.z);
                if (cp.flipposset != null)
                {
                    Undo.RecordObject(cp.flipposset, "Fix FlipOffset");
                    cp.flipposset.localPosition = new Vector3(0, 0.5f, 0);
                }
                EditorUtility.SetDirty(cp.gameObject);
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log("All checkpoints Y snapped.");
    }

    private void SnapAllCollectiblesToRoad()
    {
        foreach (var ci in allCollectibles)
        {
            Undo.RecordObject(ci.transform, "Snap Y");
            Vector3 pos = ci.transform.position;
            float y = RaycastRoadY(pos);
            if (y > -100f)
            {
                ci.transform.position = new Vector3(pos.x, y + 0.4f, pos.z);
                EditorUtility.SetDirty(ci.gameObject);
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log("All collectibles Y snapped.");
    }

    private void RefreshAllFlipOffsetsFromTangent()
    {
        var sr = GameObject.Find("SR_Checkpoints");
        if (sr == null) return;
        int n = sr.transform.childCount;
        for (int i = 0; i < n; i++)
        {
            var cpTrans = sr.transform.GetChild(i);
            var cp = cpTrans.GetComponent<SR_Checkpoint>();
            if (cp == null) continue;
            var flip = cpTrans.Find("FlipOffset");
            if (flip == null) continue;
            Vector3 avg = GetTangentDir(cp);
            if (avg.sqrMagnitude < 0.001f) avg = Vector3.forward;
            if (avg.sqrMagnitude > 0.001f)
            {
                Undo.RecordObject(flip, "Set Tangent");
                // Compensate parent rotation: localRot = Inverse(parentRot) * targetWorldRot
                Quaternion targetWorldRot = Quaternion.LookRotation(avg);
                Quaternion parentRot = cpTrans.rotation;
                flip.localRotation = Quaternion.Inverse(parentRot) * targetWorldRot;
                EditorUtility.SetDirty(flip.gameObject);
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshLists();
        Debug.Log("All FlipOffsets updated from tangent.");
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private float RaycastRoadY(Vector3 worldPos)
    {
        var hits = Physics.RaycastAll(worldPos + Vector3.up * 50f, Vector3.down, 100f);
        foreach (var h in hits)
        {
            if (h.collider.isTrigger) continue;
            if (h.collider.GetComponentInParent<SR_Checkpoint>() != null) continue;
            if (h.collider.GetComponentInParent<SR_CollectibleItem>() != null) continue;
            return h.point.y;
        }
        return -999f;
    }

    private Vector3 GetTangentDir(SR_Checkpoint cp)
    {
        if (allCheckpoints.Count < 2) return Vector3.forward;
        int idx = allCheckpoints.IndexOf(cp);
        if (idx < 0) return Vector3.forward;
        int pi = (idx - 1 + allCheckpoints.Count) % allCheckpoints.Count;
        int ni = (idx + 1) % allCheckpoints.Count;
        Vector3 d1 = (cp.transform.position - allCheckpoints[pi].transform.position).normalized; d1.y = 0;
        Vector3 d2 = (allCheckpoints[ni].transform.position - cp.transform.position).normalized; d2.y = 0;
        return (d1 + d2).normalized;
    }
}
