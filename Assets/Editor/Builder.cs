using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Builder
{
    private static void BuildWin64(BuildTarget architecture)
    {
        // Set architecture in BuildSettings
        EditorUserBuildSettings.selectedStandaloneTarget = architecture;

        // Setup build options (e.g. scenes, build output location)
        var options = new BuildPlayerOptions
        {
            // Change to scenes from your project
            scenes = new[]
            {
                "Assets/Scenes/SampleScene.unity",
            },
            // Change to location the output should go
            locationPathName = "/home/stefan_moldoveanu23/disk-1/poli-server/Build/WindowsPlayer.exe",
            options = BuildOptions.CleanBuildCache | BuildOptions.StrictMode,
            target = BuildTarget.StandaloneWindows64
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build successful - Build written to {options.locationPathName}");
        }
        else if (report.summary.result == BuildResult.Failed)
        {
            Debug.LogError($"Build failed");
        }
    }

    // This function will be called from the build process
    public static void Build()
    {
        // Build Windows64 Unity player
        BuildWin64(BuildTarget.StandaloneWindows64);
    }
}
