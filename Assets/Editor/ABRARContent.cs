using IVLab.ABREngine;
using UnityEditor;
using UnityEngine;

// ABRAR's content, added to each new scene: an ABREngine, a child of the room
// anchor at a fixed room position, so every viewer sees the data in the same
// place (it fits the data to -1..1, so a scale of 0.5 makes it 1 m across);
// and ABRARDataManager, which points it at the room's DataManager and caches
// what it sends. To place the data elsewhere, move ABR in the scene.
// Quest scenes also get ABRARStatusToggle (the guides hide once the room is
// anchored; Y shows or hides them).
public static class ABRARContent
{
    // Room frame (meters, y up), the virtual room the room's base config
    // anchors the physical room to (AprilTags room_base.json): origin on the
    // floor below the reference tag, +X to the right facing it, +Z into its
    // wall. The data's center: 1.4 m up, 2 m out into the room from that wall.
    private static readonly Vector3 DataCenter = new(0f, 1.4f, -2f);
    private const float DataSize = 1f;

    private const string ConfigPath = "Assets/ABRConfig.asset";

    public static void Add(string scenePath, AprilTagsSceneParts parts)
    {
        var abr = new GameObject("ABR");
        abr.transform.SetParent(parts.Anchor.transform, false);
        abr.transform.localPosition = DataCenter;
        abr.transform.localScale = Vector3.one * (DataSize / 2f);
        var engine = abr.AddComponent<ABREngine>();
        engine.Config = LoadOrCreateConfig();

        var link = parts.Localizer.gameObject.AddComponent<ABRARDataManager>();
        var so = new SerializedObject(link);
        so.FindProperty("localizer").objectReferenceValue = parts.Localizer;
        so.FindProperty("engine").objectReferenceValue = engine;
        so.ApplyModifiedPropertiesWithoutUndo();

        var status = GameObject.Find("Status Text")?.GetComponent<TMPro.TextMeshPro>();
        if (status != null && scenePath.Contains("Quest"))
        {
            var toggle = new SerializedObject(status.gameObject.AddComponent<ABRARStatusToggle>());
            toggle.FindProperty("statusText").objectReferenceValue = status;
            toggle.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static ABRConfig LoadOrCreateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<ABRConfig>(ConfigPath);
        if (config != null)
        {
            return config;
        }
        config = ScriptableObject.CreateInstance<ABRConfig>();
        config.mediaPath = "";   // persistentDataPath/media
        config.serverUrl = "";
        config.stateFile = "";
        config.loadStateOnStart = "";
        config.defaultGlyph = Resources.Load<GameObject>("DefaultSphere");
        config.defaultColor = Color.white;
        config.hiliteColor = Color.red;
        config.defaultNanColor = Color.yellow;
        config.scale = 1;
        // ABRARDataManager sets the DataManager and the cache at run time.
        config.dataManager = new ABRConfig.DataManagerOptions { discovery = true, fitToBounds = true, followLive = true };
        config.picking = false;
        AssetDatabase.CreateAsset(config, ConfigPath);
        return config;
    }
}
