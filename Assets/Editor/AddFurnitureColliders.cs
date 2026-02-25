using UnityEngine;
using UnityEditor;

public class AddFurnitureColliders
{
    [MenuItem("Tools/Add Furniture Colliders to Scene")]
    static void AddColliders()
    {
        int added = 0;
        int skipped = 0;

        foreach (GameObject go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!go.name.StartsWith("wood_props_")) continue;

            if (go.GetComponent<MeshCollider>() != null)
            {
                skipped++;
                continue;
            }

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            EditorUtility.SetDirty(go);
            added++;
        }

        Debug.Log($"[FurnitureColliders] Added MeshColliders to {added} objects. ({skipped} already had one)");
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
    }
}
