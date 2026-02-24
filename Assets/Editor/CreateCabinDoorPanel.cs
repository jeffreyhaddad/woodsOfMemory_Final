using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CreateCabinDoorPanel
{
    [MenuItem("Tools/Create Cabin Door Panel")]
    static void CreateDoorPanel()
    {
        GameObject cabinDoor = GameObject.Find("Cabin_Door");
        if (cabinDoor == null)
        {
            EditorUtility.DisplayDialog("Not Found",
                "Could not find 'Cabin_Door' in the scene. Make sure TerrainScene is open.", "OK");
            return;
        }

        // Remove any existing panel so we don't double-up
        Transform existing = cabinDoor.transform.Find("CabinDoorPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        // --- Door panel cube ---
        // Parented to Cabin_Door so it rotates with the whole door.
        // Offset x by +0.5 so the LEFT edge (hinge side) sits at Cabin_Door's origin pivot.
        // Height is 2 units centered at y=1 (adjust to match door frame).
        // Depth is thin (0.1) to not block the opening.
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "CabinDoorPanel";
        panel.transform.SetParent(cabinDoor.transform, false);
        panel.transform.localPosition = new Vector3(0.5f, 1f, 0f);
        panel.transform.localScale    = new Vector3(1f, 2f, 0.1f);

        // Apply the same wood material as the cabin if available
        Renderer rend = panel.GetComponent<Renderer>();
        string[] matGuids = AssetDatabase.FindAssets("Cabin t:Material", new[] { "Assets/ThirdParty/Cabin" });
        if (matGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(matGuids[0]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) rend.sharedMaterial = mat;
        }

        // BoxCollider is already added by CreatePrimitive — leave it as-is.
        // PlayerInteraction hits this collider, then GetComponentInParent<Interactable>()
        // walks up to Cabin_Door and finds CabinDoorInteractable there.

        // --- Interactable on the door pivot ---
        CabinDoorInteractable interactable = cabinDoor.GetComponent<CabinDoorInteractable>();
        if (interactable == null)
            interactable = cabinDoor.AddComponent<CabinDoorInteractable>();

        EditorUtility.SetDirty(cabinDoor);
        EditorSceneManager.MarkSceneDirty(cabinDoor.scene);

        // Select the panel so the user can immediately resize/reposition it in the scene
        Selection.activeGameObject = panel;

        Debug.Log("[CreateCabinDoorPanel] Door panel created. Adjust its localPosition/localScale in the Inspector to fit the door frame.");
        EditorUtility.DisplayDialog("Done",
            "CabinDoorPanel created as a child of Cabin_Door.\n\n" +
            "1. The panel is now selected — resize & reposition it to fit the door frame.\n" +
            "2. CabinDoorInteractable has been added to Cabin_Door.\n" +
            "3. Adjust 'Open Angle' on CabinDoorInteractable if the door swings the wrong way (try -90).",
            "OK");
    }
}
