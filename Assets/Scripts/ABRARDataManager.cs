using System;
using System.IO;
using IVLab.ABREngine;
using UnityEngine;

// ABRAR's data comes through a DataManager inside the app, whose store caches
// the room's DataManager: what it sends is kept on the device, and shown from
// there while it can't be reached (DataManager SPEC.md 15). The room config (or
// a scanned room code) names the DataManager ("host" or "host:port"); without
// one it's found by discovery, which also finds one restarted on another machine.
//
// The store: on Quest /sdcard/DataManager/store, shared with PVAR and the
// DataManager example apps (needs All files access, which the AprilTags Quest
// build declares; grant it once with adb shell appops set edu.utexas.tacc.abrar
// MANAGE_EXTERNAL_STORAGE allow); elsewhere a store in the app's own folder.
public class ABRARDataManager : MonoBehaviour
{
    [SerializeField] private AprilTagRoomLocalizer localizer;
    [SerializeField] private ABREngine engine;

    public const string QuestStore = "/sdcard/DataManager/store";

    private string current;
    private bool configCopied;

    public static string StorePath =>
        Application.platform == RuntimePlatform.Android
            ? QuestStore
            : Path.Combine(Application.persistentDataPath, "store");

    private void OnEnable()
    {
        localizer.ConfigChanged += OnConfigChanged;
        if (localizer.IsConfigLoaded)
        {
            OnConfigChanged(localizer.LoadedConfig);
        }
        else
        {
            Use("");   // discovery, until a room names a DataManager
        }
    }

    private void OnDisable() => localizer.ConfigChanged -= OnConfigChanged;

    private void OnConfigChanged(RoomConfig config)
    {
        var address = (config.dataManager ?? "").Trim();
        if (address == current)
        {
            return;
        }
        Debug.Log(address.Length > 0
            ? $"[ABRARDataManager] Room '{config.name}' names DataManager {address}"
            : $"[ABRARDataManager] Room '{config.name}' names no DataManager; finding one by discovery");
        Use(address);
    }

    private void Use(string address)
    {
        current = address;
        var host = address;
        var port = 9010;
        var colon = address.LastIndexOf(':');
        if (colon > 0 && int.TryParse(address[(colon + 1)..], out var parsed))
        {
            host = address[..colon];
            port = parsed;
        }
        var store = StorePath;
        var cache = true;
        try
        {
            Directory.CreateDirectory(store);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ABRARDataManager] Can't use the store {store} ({e.Message}); connecting without a cache");
            cache = false;
        }
        var options = new ABRConfig.DataManagerOptions
        {
            host = host,
            port = port,
            discovery = host.Length == 0,
            store = cache ? store : "",
            cache = cache,
            fitToBounds = true,
            followLive = true,
        };
        Debug.Log($"[ABRARDataManager] {(cache ? "Caching" : "Connecting to")} " +
                  $"{(host.Length > 0 ? $"{host}:{port}" : "a discovered DataManager")}{(cache ? $" in {store}" : "")}");

        // The engine is under the room, which stays inactive until it's
        // anchored; before it wakes, its config says what to start with.
        if (!engine.isActiveAndEnabled)
        {
            if (!configCopied)
            {
                engine.Config = Instantiate(engine.Config);   // not the project's asset
                configCopied = true;
            }
            engine.Config.dataManager = options;
            engine.Config.picking = false;
            return;
        }
        engine.SetDataSource(options);
    }
}
