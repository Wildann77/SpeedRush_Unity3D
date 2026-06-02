using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class DebugSceneItems
{
    [InitializeOnLoadMethod]
    public static void Initialize()
    {
        // Defer execution using EditorApplication.update to avoid early-load URP NullReferenceException
        EditorApplication.update += CheckTriggerDeferred;
    }

    private static void CheckTriggerDeferred()
    {
        EditorApplication.update -= CheckTriggerDeferred; // Unsubscribe immediately
        
        string triggerPath = "Temp/debug_scene.trigger";
        if (!File.Exists(triggerPath)) return;
        
        try
        {
            File.Delete(triggerPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to delete trigger: " + e.Message);
        }

        RunDebug();
    }

    private static void RunDebug()
    {
        string scenePath = "Assets/Scenes/Level1.unity";
        Debug.Log("--- LOADING SCENE FOR ROAD SCAN DEFERRED ---");
        
        // Save current scene changes if any
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Find the F1 RaceTrack or track object
        GameObject trackObj = GameObject.Find("F1 RaceTrack");
        if (trackObj == null)
        {
            // Search by name
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.Contains("Track") || go.name.Contains("track") || go.name.Contains("Race"))
                {
                    trackObj = go;
                    break;
                }
            }
        }
        
        if (trackObj != null)
        {
            Debug.Log("Found track object: " + trackObj.name);
            PrintChildrenRecursive(trackObj.transform, 0, 4); // print up to 4 levels
        }
        else
        {
            Debug.LogWarning("Track object NOT found in hierarchy!");
        }

        // Print checkpoints
        var checkpoints = Object.FindObjectsByType<SpeedRush.SR_Checkpoint>(FindObjectsSortMode.None);
        Debug.Log("Found " + checkpoints.Length + " SR_Checkpoints in scene:");
        foreach (var cp in checkpoints)
        {
            Debug.Log("  Checkpoint GameObject: " + cp.name + ", Position: " + cp.transform.position);
        }

        // Print collectibles count
        var collectibles = Object.FindObjectsByType<SpeedRush.SR_CollectibleItem>(FindObjectsSortMode.None);
        Debug.Log("Found " + collectibles.Length + " SR_CollectibleItems in scene.");
    }

    private static void PrintChildrenRecursive(Transform t, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        string indent = new string('-', depth * 2);
        Debug.Log(indent + " " + t.name + " (Active: " + t.gameObject.activeSelf + ", Children: " + t.childCount + ")");
        
        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Debug.Log(indent + "  [Mesh: " + mf.sharedMesh.name + ", Verts: " + mf.sharedMesh.vertexCount + "]");
        }
        
        for (int i = 0; i < t.childCount; i++)
        {
            PrintChildrenRecursive(t.GetChild(i), depth + 1, maxDepth);
        }
    }
}
