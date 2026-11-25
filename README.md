# ABRAR

ABR in shared AR. This is the [Locator](https://github.com/GregAbram/Locator)
with ABR: data from a [DataManager](https://github.com/GregAbram/DataManager),
styled in `abr_server` (colormaps, glyphs, textures), appears at a fixed place
in the room, the same place for every viewer on Quest 3 and iPhone. It's the
ABR counterpart of [PVAR](https://github.com/GregAbram/PVAR), which shows the
same data as ParaView colored it.

Unity 6000.3.10f1, with [edu.tacc.apriltags](https://github.com/GregAbram/AprilTags),
and ABREngine and the DataManager package from the DataManager repository
(pinned to one commit in `Packages/manifest.json`).

## Using it

1. Find the room as with Locator. On a Quest where the room was surveyed with
   Survey, that's automatic; otherwise scan the room code. Then look at two
   tags. The room's guides (status, tag markers, reference posts) hide 3 s
   after it's anchored; **Y** brings them back.
2. Run a DataManager and `abr_server` (see the DataManager user guide), and
   style the current state. ABRAR finds the DataManager by discovery, also when
   it's restarted on another machine; a room code that names one ("host" or
   "host:port") is used instead.
3. **Right thumbstick:** left and right step through time, for every app on the
   state; up and down switch this headset to another state.

ABRAR keeps what the DataManager sends in a store on the device
(`/sdcard/DataManager/store` on Quest, shared with PVAR and the DataManager
example apps), and goes on showing it when the DataManager can't be reached; a
label says so. Only timesteps that have been shown are kept. On Quest the app
needs All files access for that store and for the shared room, once per
install:

```bash
adb shell appops set edu.utexas.tacc.abrar MANAGE_EXTERNAL_STORAGE allow
```

To place the data elsewhere, move `ABR` (under `Room`) in the scene. It's 1 m
across: ABR fits the data to -1..1, and `ABR` is scaled 0.5.

## Building

Menu **Tools > ABRAR**, or batchmode `ABRARApp.BuildIOS` / `ABRARApp.BuildQuest`
(see the Locator README). Bundle ID `edu.utexas.tacc.abrar`. New scenes get
their ABR objects from `Assets/Editor/ABRARContent.cs`. Quest builds use MSAA
off, no shadows and a 0.8x resolution limit (AprilTags README: Performance on
Quest).
