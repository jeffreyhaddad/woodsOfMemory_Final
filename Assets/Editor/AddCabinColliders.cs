using UnityEngine;
using UnityEditor;

public static class AddCabinColliders
{
    [MenuItem("Tools/Add Colliders to Mountain Cabin")]
    static void AddColliders()
    {
        GameObject cabin = GameObject.Find("MoutainCabin");
        if (cabin == null)
        {
            EditorUtility.DisplayDialog("Not Found", "Could not find 'MoutainCabin' in the scene. Make sure TerrainScene is open.", "OK");
            return;
        }

        MeshFilter[] filters = cabin.GetComponentsInChildren<MeshFilter>(true);
        int added = 0;
        int skipped = 0;

        foreach (MeshFilter mf in filters)
        {
            // Skip window glass — no solid collision needed
            if (mf.gameObject.name.Contains("Window_Glass"))
            {
                skipped++;
                continue;
            }

            if (mf.gameObject.GetComponent<Collider>() != null)
            {
                skipped++;
                continue;
            }

            MeshCollider col = mf.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = mf.sharedMesh;
            added++;
        }

        EditorUtility.SetDirty(cabin);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cabin.scene);

        Debug.Log($"[AddCabinColliders] Added MeshCollider to {added} objects, skipped {skipped} (already had collider or window glass).");
        EditorUtility.DisplayDialog("Done", $"Added MeshCollider to {added} mesh(es).\nSkipped {skipped} (window glass or already had collider).", "OK");
    }
}
