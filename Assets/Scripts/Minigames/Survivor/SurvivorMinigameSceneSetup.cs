using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SurvivorMinigameSceneSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/Saturn/Create Survivor Minigame Waypoint", false, 10)]
    private static void CreateWaypoint()
    {
        GameObject waypoint = new GameObject("SurvivorMinigameWaypoint");
        BoxCollider trigger = waypoint.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(3f, 3f, 3f);

        MinigameWaypointTrigger waypointTrigger = waypoint.AddComponent<MinigameWaypointTrigger>();

        SurvivorMinigameController controller = Object.FindFirstObjectByType<SurvivorMinigameController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("SurvivorMinigame");
            controller = controllerObject.AddComponent<SurvivorMinigameController>();
        }

        waypointTrigger.minigameController = controller;

        SurvivorMinigameConfig config = ScriptableObject.CreateInstance<SurvivorMinigameConfig>();
        config.name = "DefaultSurvivorConfig";
        waypointTrigger.minigameConfig = config;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "WaypointMarker";
        marker.transform.SetParent(waypoint.transform, false);
        marker.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
        marker.transform.localPosition = Vector3.up;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.GetComponent<Renderer>().sharedMaterial.color = new Color(0.95f, 0.75f, 0.2f);

        Selection.activeGameObject = waypoint;
        Undo.RegisterCreatedObjectUndo(waypoint, "Create Survivor Minigame Waypoint");
    }
#endif
}
