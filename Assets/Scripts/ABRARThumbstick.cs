using System;
using IVLab.ABREngine;
using TMPro;
using UnityEngine;

// Quest: the right thumbstick moves through the data, as in the DataManager
// examples. Right and left step to the next or previous timestep (a flick
// moves one, holding repeats; stepping stops Play) - for every app following
// the state. Up and down switch this headset to the previous or next of the
// DataManager's states, once per push; a label below the line of sight names
// the state for a few seconds. The same label says when the DataManager can't
// be reached (cached data is shown) and when it's back. Made at startup on an
// object of its own, for the scene's ABREngine (which is inactive until the
// room is anchored), so scenes need no change.
public class ABRARThumbstick : MonoBehaviour
{
    private ABREngine engine;
    private int stickDirection;   // ±1 time, ±2 state, 0 centred
    private float repeatAt;
    private string shownState = "";
    private bool? shownOnline;
    private TextMeshPro label;
    private float labelUntil;

#if UNITY_ANDROID
    // Quest only: OVRInput.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var engine = FindAnyObjectByType<ABREngine>(FindObjectsInactive.Include);
        if (engine == null)
        {
            Debug.LogWarning("[ABRARThumbstick] No ABREngine in the scene; the thumbstick does nothing");
            return;
        }
        var go = new GameObject("ABRAR Thumbstick");
        go.AddComponent<ABRARThumbstick>().engine = engine;
        Debug.Log("[ABRARThumbstick] Right thumbstick: left/right step time, up/down switch state");
    }

    private void Update()
    {
        var v = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        var horizontal = Mathf.Abs(v.x) >= Mathf.Abs(v.y);
        var a = horizontal ? v.x : v.y;
        var direction = a > 0.6f ? 1 : a < -0.6f ? -1 : 0;
        if (direction == 0)
        {
            if (v.magnitude < 0.3f)
            {
                stickDirection = 0;
            }
        }
        else
        {
            var code = horizontal ? direction : 2 * direction;
            var now = Time.unscaledTime;
            if (code != stickDirection || horizontal && now >= repeatAt)
            {
                repeatAt = now + (code == stickDirection ? 0.15f : 0.4f);
                stickDirection = code;
                if (horizontal)
                {
                    StepTime(direction);
                }
                else
                {
                    NextState(-direction);   // up: previous
                }
            }
        }
        ShowChanges();
        LogScene();
    }
#endif

    // Diagnostics (temporary): every 5 s, what ABR is showing and where.
    private float nextSceneLog;

    private void LogScene()
    {
        if (Time.unscaledTime < nextSceneLog || engine == null)
        {
            return;
        }
        nextSceneLog = Time.unscaledTime + 5f;
        var source = Source;
        var impressions = engine.isActiveAndEnabled ? engine.GetAllDataImpressions()?.Count ?? -1 : -1;
        int renderers = 0, shown = 0;
        Bounds? bounds = null;
        foreach (var r in engine.GetComponentsInChildren<Renderer>(true))
        {
            renderers++;
            if (r.enabled && r.gameObject.activeInHierarchy)
            {
                shown++;
                if (bounds == null) bounds = r.bounds;
                else { var b = bounds.Value; b.Encapsulate(r.bounds); bounds = b; }
            }
        }
        var head = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        var t = engine.transform;
        Debug.Log($"[ABRARScene] engine active {engine.isActiveAndEnabled}, source {(source == null ? "none" : "yes")}, " +
                  $"state '{source?.State?.Name}' abr {(source?.State?.Json?.Contains("\"abr\"") ?? false)}, " +
                  $"timesteps {source?.Timesteps?.Length ?? 0}, t {source?.CurrentTime:G4}; impressions {impressions}, " +
                  $"renderers {renderers} ({shown} shown); ABR at {t.position} scale {t.lossyScale.x:0.###}; " +
                  (bounds.HasValue ? $"shown bounds center {bounds.Value.center} size {bounds.Value.size}; " : "") +
                  $"head {head}; config center {engine.Config.center} scale {engine.Config.scale:G4}");
    }

    private DMDataSource Source => engine != null ? engine.DataSource : null;

    private void StepTime(int delta)
    {
        var source = Source;
        var times = source?.Timesteps;
        if (times == null || times.Length == 0 || source.Client == null)
        {
            return;
        }
        if (source.Client.Time.Sweeping)
        {
            source.Pause();
        }
        var i = Array.BinarySearch(times, source.CurrentTime);
        if (i < 0)
        {
            i = Math.Max(0, ~i - (delta > 0 ? 1 : 0));
        }
        source.Client.MoveTo(times[Mathf.Clamp(i + delta, 0, times.Length - 1)]);
    }

    private string[] SortedStates()
    {
        var names = (string[])(Source?.StateNames ?? new string[0]).Clone();
        Array.Sort(names, StringComparer.OrdinalIgnoreCase);
        return names;
    }

    private void NextState(int delta)
    {
        var source = Source;
        if (source == null)
        {
            return;
        }
        source.ListStates();   // so a state made since is there next time
        var names = SortedStates();
        var current = source.State?.Name ?? "";
        if (names.Length == 0)
        {
            ShowLabel("No states yet");
            return;
        }
        var i = Array.IndexOf(names, current);
        if (names.Length == 1 && i == 0)
        {
            ShowLabel($"{current}\n(the only state)");
            return;
        }
        var next = i < 0 ? 0 : (i + delta + names.Length) % names.Length;
        source.SetState(names[next]);
        ShowLabel($"{names[next]}\n{next + 1} of {names.Length}, loading");
    }

    // The state whenever it changes, and the DataManager going away or coming back.
    private void ShowChanges()
    {
        var source = Source;
        var name = source?.State?.Name ?? "";
        if (name != shownState && name.Length > 0)
        {
            shownState = name;
            var names = SortedStates();
            var i = Array.IndexOf(names, name);
            ShowLabel(i >= 0 && names.Length > 1 ? $"{name}\n{i + 1} of {names.Length}" : name);
        }
        var upstream = source?.Client?.UpstreamStatus;
        if (upstream.HasValue && upstream.Value.IsOnline != shownOnline)
        {
            var wasKnown = shownOnline.HasValue;
            shownOnline = upstream.Value.IsOnline;
            if (!upstream.Value.IsOnline)
            {
                // "discovery": it was found by discovery, and can't be found now.
                var who = upstream.Value.Offline == "discovery" ? "the DataManager can't be found"
                                                                : $"{upstream.Value.Offline} can't be reached";
                ShowLabel($"Offline: showing cached data\n{who}", 6f);
            }
            else if (wasKnown)
            {
                ShowLabel("DataManager online again");
            }
        }
        if (label != null && label.enabled && Time.unscaledTime > labelUntil)
        {
            label.enabled = false;
        }
    }

    private void ShowLabel(string text, float seconds = 3f)
    {
        if (label == null)
        {
            var head = Camera.main != null ? Camera.main.transform : null;
            if (head == null)
            {
                return;
            }
            var go = new GameObject("State Label");
            go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0f, -0.15f, 0.8f);
            label = go.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(0.6f, 0.2f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSize = 0.3f;
            // Match the status panel's look where there is one.
            var status = GameObject.Find("Status Text")?.GetComponent<TextMeshPro>();
            if (status != null)
            {
                label.font = status.font;
                label.color = status.color;
            }
        }
        label.text = text;
        label.enabled = true;
        labelUntil = Time.unscaledTime + seconds;
    }
}
