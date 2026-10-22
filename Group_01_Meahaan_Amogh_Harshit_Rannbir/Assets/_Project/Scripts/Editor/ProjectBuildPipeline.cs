// ProjectBuildPipeline.cs - Automated Cross-Platform Build Pipeline
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Builds Windows Standalone (Desktop/PCVR) and Android Standalone (Meta Quest 2).

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class ProjectBuildPipeline
{
    private static readonly string[] BuildScenes = new string[]
    {
        "Assets/Scenes/SampleScene.unity"
    };

    [MenuItem("IVRAR/Build/Build Windows Standalone (Desktop & PCVR)", false, 10)]
    public static BuildReport BuildWindowsStandalone()
    {
        string buildDir = Path.Combine(Application.dataPath, "..", "Builds", "Windows");
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

        string exePath = Path.Combine(buildDir, "IVRAR_Group01_Simulator.exe");

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = BuildScenes;
        options.locationPathName = exePath;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        Debug.Log($"[ProjectBuildPipeline] Initiating Windows Standalone build -> {exePath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        LogBuildReport("Windows Standalone", report);
        return report;
    }

    [MenuItem("IVRAR/Build/Build Meta Quest 2 Standalone (Android APK)", false, 11)]
    public static BuildReport BuildMetaQuestAPK()
    {
        string buildDir = Path.Combine(Application.dataPath, "..", "Builds", "Quest");
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

        string apkPath = Path.Combine(buildDir, "IVRAR_Group01_Quest2.apk");

        // Ensure ARM64 and OpenXR / Quest target settings
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false; // Generate .apk for sideloading / testing

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = BuildScenes;
        options.locationPathName = apkPath;
        options.target = BuildTarget.Android;
        options.options = BuildOptions.None;

        Debug.Log($"[ProjectBuildPipeline] Initiating Meta Quest Android build -> {apkPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        LogBuildReport("Meta Quest 2 Android", report);
        return report;
    }

    private static void LogBuildReport(string platformName, BuildReport report)
    {
        BuildSummary summary = report.summary;
        Debug.Log($"[ProjectBuildPipeline] {platformName} Build Finished. Result: {summary.result}");
        Debug.Log($"[ProjectBuildPipeline] Total Time: {summary.totalTime}, Total Size: {summary.totalSize / (1024 * 1024)} MB");
        Debug.Log($"[ProjectBuildPipeline] Total Errors: {summary.totalErrors}, Total Warnings: {summary.totalWarnings}");

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[ProjectBuildPipeline] SUCCESS: Build produced at: {summary.outputPath}");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[ProjectBuildPipeline] FAILED: Build encountered {summary.totalErrors} errors.");
        }
    }
}
#endif
