using UnityEditor;

// ABRAR: the Locator with ABR's styled data from a DataManager, placed in the
// shared room at a fixed room position (see ABRARContent).
// Menu: Tools > ABRAR. Batchmode (re-runnable; Build also configures):
//   -buildTarget iOS -executeMethod ABRARApp.BuildIOS        (Xcode project in Builds/iOS)
//   -buildTarget Android -executeMethod ABRARApp.BuildQuest  (Builds/Quest/ABRAR.apk)
public static class ABRARApp
{
    public static readonly AprilTagsAppInfo App = new()
    {
        ProductName = "ABRAR",
        BundleId = "edu.utexas.tacc.abrar",
        AppleTeamId = "MQSWMVW9A8",
        LogFileName = "abrar.log",
        IPhoneScenes = new[] { "Assets/Scenes/iPhone ABRAR.unity" },
        QuestScenes = new[] { "Assets/Scenes/Quest ABRAR.unity" },
        // Dense data close up: no supersampling, and room to drop below 1.
        QuestMaxResolutionScale = 0.8f,
    };

    [MenuItem("Tools/ABRAR/Configure iOS")]
    public static void ConfigureIOS() => AprilTagsIOSSetup.Configure(App, ABRARContent.Add);

    [MenuItem("Tools/ABRAR/Build iOS Xcode Project")]
    public static void BuildIOS()
    {
        ConfigureIOS();
        AprilTagsIOSSetup.Build(App);
    }

    [MenuItem("Tools/ABRAR/Configure Quest")]
    public static void ConfigureQuest() => AprilTagsQuestSetup.Configure(App, ABRARContent.Add);

    [MenuItem("Tools/ABRAR/Build Quest APK")]
    public static void BuildQuest()
    {
        ConfigureQuest();
        AprilTagsQuestSetup.Build(App);
    }
}
