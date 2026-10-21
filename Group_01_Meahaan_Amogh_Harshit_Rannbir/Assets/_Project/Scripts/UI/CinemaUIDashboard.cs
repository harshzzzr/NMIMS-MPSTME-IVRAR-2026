// CinemaUIDashboard.cs - Dual VR World-Space Canvas and Desktop HUD
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Provides live scientific readouts, scenario toggles, subwoofer status, and telemetry export.

using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class CinemaUIDashboard : MonoBehaviour
{
    [Header("Subsystem References")]
    public EnvironmentModifier environmentModifier;
    public OpticsErgonomicsCalculator opticsCalculator;
    public VirtualSPLMeter splMeter;
    public SubwooferController subwoofer;
    public MovieScreenPlayer moviePlayer;
    public AcousticRaycaster raycaster;
    public RT60TelemetryLogger telemetryLogger;
    public PlatformManager platformManager;

    [Header("Desktop HUD Overlay Settings")]
    public bool showDesktopHUD = true;
    public KeyCode toggleHudKey = KeyCode.Tab;

    [Header("Optional World-Space Canvas UGUI Elements")]
    public Text scenarioText;
    public Text rt60MetricsText;
    public Text cediaComplianceText;
    public Text opticalMetricsText;
    public Text splMetricsText;
    public Text subwooferStatusText;
    public Text movieStatusText;

    private void Awake()
    {
        FindSubsystemsIfMissing();
    }

    private void Start()
    {
        UpdateUIDisplay();
    }

    private void Update()
    {
        bool toggleRequested = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.tabKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame)
            {
                toggleRequested = true;
            }
        }
        else
#endif
        {
            try
            {
                if (Input.GetKeyDown(toggleHudKey) || Input.GetKeyDown(KeyCode.H))
                {
                    toggleRequested = true;
                }
            }
            catch { /* Ignore legacy input exception */ }
        }

        if (toggleRequested)
        {
            showDesktopHUD = !showDesktopHUD;
        }

        UpdateUIDisplay();
    }

    public void FindSubsystemsIfMissing()
    {
        if (environmentModifier == null) environmentModifier = FindAnyObjectByType<EnvironmentModifier>();
        if (opticsCalculator == null) opticsCalculator = FindAnyObjectByType<OpticsErgonomicsCalculator>();
        if (splMeter == null) splMeter = FindAnyObjectByType<VirtualSPLMeter>();
        if (subwoofer == null) subwoofer = FindAnyObjectByType<SubwooferController>();
        if (moviePlayer == null) moviePlayer = FindAnyObjectByType<MovieScreenPlayer>();
        if (raycaster == null) raycaster = FindAnyObjectByType<AcousticRaycaster>();
        if (telemetryLogger == null) telemetryLogger = FindAnyObjectByType<RT60TelemetryLogger>();
        if (platformManager == null) platformManager = FindAnyObjectByType<PlatformManager>();
    }

    /// <summary>
    /// Refreshes text components on world-space Canvas if assigned.
    /// </summary>
    public void UpdateUIDisplay()
    {
        if (environmentModifier == null) return;

        if (scenarioText != null)
        {
            scenarioText.text = $"Scenario: {environmentModifier.currentScenario}";
        }

        if (rt60MetricsText != null)
        {
            rt60MetricsText.text = $"Sabine RT60: {environmentModifier.SabineRT60:F2}s\n" +
                                   $"Eyring RT60: {environmentModifier.EyringRT60:F2}s\n" +
                                   $"Schroeder RT60: {environmentModifier.SchroederRT60:F2}s";
        }

        if (cediaComplianceText != null)
        {
            cediaComplianceText.text = environmentModifier.IsCEDIACompliant ? "CEDIA RP22: PASS [0.30 - 0.50s]" : "CEDIA RP22: NON-COMPLIANT";
            cediaComplianceText.color = environmentModifier.IsCEDIACompliant ? Color.green : Color.red;
        }

        if (opticalMetricsText != null && opticsCalculator != null)
        {
            opticalMetricsText.text = $"Viewing Dist: {opticsCalculator.ViewingDistanceM:F2}m\n" +
                                      $"Horiz FOV: {opticsCalculator.HorizontalFOV:F1}° (SMPTE 36-40°)\n" +
                                      $"Sightline Clearance: {opticsCalculator.SightlineClearancePercent:F0}%";
        }

        if (splMetricsText != null && splMeter != null)
        {
            splMetricsText.text = $"Live SPL: {splMeter.CurrentDecibels:F1} dB\n" +
                                  $"Row Balance: +/- {splMeter.LevelVariationDeltaDb:F1} dB";
        }

        if (subwooferStatusText != null && subwoofer != null)
        {
            Vector3 pos = subwoofer.transform.position;
            subwooferStatusText.text = $"Subwoofer Pos: ({pos.x:F2}, {pos.z:F2})";
        }

        if (movieStatusText != null && moviePlayer != null)
        {
            movieStatusText.text = $"Movie Status: {moviePlayer.PlaybackStatus}";
        }
    }

    // =========================================================================
    // BUTTON HANDLERS (Invoked by UI buttons or ray interactors)
    // =========================================================================

    public void OnSelectScenarioA()
    {
        if (environmentModifier != null) environmentModifier.ApplyScenario(EnvironmentModifier.ScenarioType.BaselineBare);
    }

    public void OnSelectScenarioB()
    {
        if (environmentModifier != null) environmentModifier.ApplyScenario(EnvironmentModifier.ScenarioType.OverTreated);
    }

    public void OnSelectScenarioC()
    {
        if (environmentModifier != null) environmentModifier.ApplyScenario(EnvironmentModifier.ScenarioType.Optimized);
    }

    public void OnToggleMoviePlayback()
    {
        if (moviePlayer == null) return;
        if (moviePlayer.IsPlaying && !moviePlayer.IsPaused) moviePlayer.PauseMovie();
        else if (moviePlayer.IsPaused) moviePlayer.ResumeMovie();
        else moviePlayer.PlayMovie();
    }

    public void OnRestartMovie()
    {
        if (moviePlayer != null) moviePlayer.RestartMovie();
    }

    public void OnRunRaycastSimulation()
    {
        if (raycaster != null)
        {
            raycaster.RunSimulation();
        }
        if (environmentModifier != null)
        {
            environmentModifier.RecalculateAcousticMetrics();
        }
    }

    public void OnExportTrialCSV()
    {
        if (telemetryLogger == null || environmentModifier == null) return;

        float fov = opticsCalculator != null ? opticsCalculator.HorizontalFOV : 38.5f;
        float gaze = opticsCalculator != null ? opticsCalculator.VerticalGazeAngle : 8.5f;
        float clearance = opticsCalculator != null ? opticsCalculator.SightlineClearancePercent : 100f;
        float dist = opticsCalculator != null ? opticsCalculator.ViewingDistanceM : 3.9f;
        float spl = splMeter != null ? splMeter.CurrentDecibels : 78.5f;
        Vector3 subPos = subwoofer != null ? subwoofer.transform.position : Vector3.zero;

        telemetryLogger.LogComprehensiveTrial(
            scenarioName: environmentModifier.currentScenario.ToString(),
            roomVolumeM3: environmentModifier.RoomVolumeM3,
            surfaceAreaM2: environmentModifier.TotalSurfaceAreaM2,
            sabineRT60: environmentModifier.SabineRT60,
            eyringRT60: environmentModifier.EyringRT60,
            schroederRT60: environmentModifier.SchroederRT60,
            viewingDistanceM: dist,
            horizontalFOV: fov,
            verticalGazeAngle: gaze,
            sightlineClearancePercent: clearance,
            currentSPL: spl,
            subwooferPosX: subPos.x,
            subwooferPosZ: subPos.z,
            notes: "Exported from UI Dashboard"
        );
    }

    // =========================================================================
    // IMGUI DESKTOP DASHBOARD (Guaranteed immediate desktop & testing overlay)
    // =========================================================================

    private void OnGUI()
    {
        if (!showDesktopHUD) return;
        // Do not draw IMGUI overlay on mobile / Quest standalone unless enabled
        if (Application.isMobilePlatform) return;

        GUILayout.BeginArea(new Rect(15, 15, 340, 560), GUI.skin.box);
        GUILayout.Label("<b>IVRAR Group 01: Home Theatre Simulator</b>", GetHeaderStyle());
        GUILayout.Label("Universal Render Pipeline · CEDIA/CTA-RP22");
        GUILayout.Space(6);

        if (platformManager != null)
        {
            GUILayout.Label($"<b>Mode:</b> {platformManager.activePlatformMode} | {platformManager.ControllerStatusMessage}");
        }

        if (environmentModifier != null)
        {
            GUILayout.Space(4);
            GUILayout.Label($"<b>Scenario:</b> <color=#ffd700>{environmentModifier.currentScenario}</color>");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("A: Bare")) OnSelectScenarioA();
            if (GUILayout.Button("B: Over-Treated")) OnSelectScenarioB();
            if (GUILayout.Button("C: Optimized")) OnSelectScenarioC();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("<b>Acoustic Decay Profile:</b>");
            GUILayout.Label($"Room Volume: {environmentModifier.RoomVolumeM3:F1} m³ | Area: {environmentModifier.TotalSurfaceAreaM2:F1} m²");
            GUILayout.Label($"Sabine RT60: <b>{environmentModifier.SabineRT60:F2}s</b>");
            GUILayout.Label($"Eyring RT60: <b>{environmentModifier.EyringRT60:F2}s</b>");
            GUILayout.Label($"Schroeder RT60: <b>{environmentModifier.SchroederRT60:F2}s</b>");

            string cediaBadge = environmentModifier.IsCEDIACompliant ? "<color=#00ff00>PASS (0.30 - 0.50s)</color>" : "<color=#ff4444>FAIL (Target 0.45s)</color>";
            GUILayout.Label($"CEDIA RP22 Standard: {cediaBadge}");
        }

        if (splMeter != null)
        {
            GUILayout.Space(4);
            GUILayout.Label($"SPL Meter: <b>{splMeter.CurrentDecibels:F1} dB</b> (Variation: ±{splMeter.LevelVariationDeltaDb:F1} dB)");
        }

        if (opticsCalculator != null)
        {
            GUILayout.Space(4);
            GUILayout.Label("<b>Viewing Ergonomics & Sightlines:</b>");
            GUILayout.Label($"Viewing Distance: {opticsCalculator.ViewingDistanceM:F2} m");
            string fovBadge = opticsCalculator.IsSMPTECompliant ? "<color=#00ff00>OK (36-40°)</color>" : "<color=#ffaa00>Uncalibrated</color>";
            GUILayout.Label($"Horizontal FOV: <b>{opticsCalculator.HorizontalFOV:F1}°</b> {fovBadge}");
            GUILayout.Label($"Vertical Elevation: <b>{opticsCalculator.VerticalGazeAngle:F1}°</b> (SMPTE <= 15°)");
            string sightlineBadge = opticsCalculator.SightlineClearancePercent >= 99f ? "<color=#00ff00>100% Clear</color>" : $"<color=#ff4444>{opticsCalculator.SightlineClearancePercent:F0}% Obstructed</color>";
            GUILayout.Label($"Sightline Clearance: {sightlineBadge}");
        }

        if (subwoofer != null)
        {
            GUILayout.Space(4);
            Vector3 sPos = subwoofer.transform.position;
            GUILayout.Label($"Subwoofer Pos: ({sPos.x:F2}, {sPos.z:F2})");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sub: Corner Node")) subwoofer.SetToBaselinePosition();
            if (GUILayout.Button("Sub: 1/3 Axial Crawl")) subwoofer.SetToOptimizedPosition();
            GUILayout.EndHorizontal();
        }

        if (moviePlayer != null)
        {
            GUILayout.Space(4);
            GUILayout.Label($"Movie: {moviePlayer.PlaybackStatus}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(moviePlayer.IsPlaying && !moviePlayer.IsPaused ? "Pause" : "Play")) OnToggleMoviePlayback();
            if (GUILayout.Button("Restart")) OnRestartMovie();
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Run Raycast")) OnRunRaycastSimulation();
        if (GUILayout.Button("Export CSV Trial")) OnExportTrialCSV();
        GUILayout.EndHorizontal();

        GUILayout.Label("<size=10>Press TAB to toggle HUD | Right-Click for Mouse Look</size>");
        GUILayout.EndArea();
    }

    private GUIStyle headerStyle;
    private GUIStyle GetHeaderStyle()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.richText = true;
            headerStyle.fontSize = 13;
        }
        return headerStyle;
    }
}
