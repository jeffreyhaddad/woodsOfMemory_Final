using UnityEngine;
using UnityEditor;

public class ScaleTerrainTrees : EditorWindow
{
    float scaleFactor = 3.0f;

    [MenuItem("Tools/Scale Terrain Trees")]
    static void ShowWindow()
    {
        GetWindow<ScaleTerrainTrees>("Scale Terrain Trees");
    }

    void OnGUI()
    {
        GUILayout.Label("Scale All Terrain Trees", EditorStyles.boldLabel);
        scaleFactor = EditorGUILayout.FloatField("Scale Factor", scaleFactor);

        if (GUILayout.Button("Preview Current Sizes"))
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                TerrainData td = t.terrainData;
                TreeInstance[] trees = td.treeInstances;
                Debug.Log($"Terrain '{t.name}': {trees.Length} trees, {td.treePrototypes.Length} prototypes");

                for (int i = 0; i < td.treePrototypes.Length; i++)
                {
                    string prefabName = td.treePrototypes[i].prefab != null
                        ? td.treePrototypes[i].prefab.name : "null";
                    Debug.Log($"  Prototype {i}: {prefabName}");
                }

                // Show first few tree scales
                int count = Mathf.Min(trees.Length, 10);
                for (int i = 0; i < count; i++)
                {
                    Debug.Log($"  Tree[{i}]: proto={trees[i].prototypeIndex}, " +
                              $"widthScale={trees[i].widthScale:F2}, " +
                              $"heightScale={trees[i].heightScale:F2}");
                }
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button($"Scale All Trees by {scaleFactor}x"))
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                TerrainData td = t.terrainData;
                TreeInstance[] trees = td.treeInstances;

                Undo.RegisterCompleteObjectUndo(td, "Scale Terrain Trees");

                for (int i = 0; i < trees.Length; i++)
                {
                    trees[i].widthScale *= scaleFactor;
                    trees[i].heightScale *= scaleFactor;
                }

                td.treeInstances = trees;
                Debug.Log($"Scaled {trees.Length} trees on '{t.name}' by {scaleFactor}x");
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Set All Trees to Exact Scale"))
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                TerrainData td = t.terrainData;
                TreeInstance[] trees = td.treeInstances;

                Undo.RegisterCompleteObjectUndo(td, "Set Terrain Tree Scale");

                for (int i = 0; i < trees.Length; i++)
                {
                    trees[i].widthScale = scaleFactor;
                    trees[i].heightScale = scaleFactor;
                }

                td.treeInstances = trees;
                Debug.Log($"Set {trees.Length} trees on '{t.name}' to scale {scaleFactor}");
            }
        }
    }
}
