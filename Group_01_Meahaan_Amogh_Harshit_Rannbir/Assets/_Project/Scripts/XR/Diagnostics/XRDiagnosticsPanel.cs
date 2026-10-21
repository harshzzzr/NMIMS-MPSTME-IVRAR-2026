// XRDiagnosticsPanel.cs - Real-time Truthful XR & Hardware Telemetry Panel
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Displays actual device tracking states, simulator detection, active camera, and frame timing.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class XRDiagnosticsPanel : MonoBehaviour
{
    [Header("Display Configuration")]
    public bool showPanel = true;
    public bool showInWorldSpace = true;
    public Vector3 worldSpacePosition = new Vector3(-1.4f, 1.15f, 0.2f);
    public Vector3 worldSpaceRotation = new Vector3(0f, 45f, 0f);

    [Header("UI Text References (Optional World-Space Canvas)")]
    public UnityEngine.UI.Text headerText;
    public UnityEngine.UI.Text runtimeStatusText;
    public UnityEngine.UI.Text hmdStatusText;
    public UnityEngine.UI.Text controllerStatusText;
    public UnityEngine.UI.Text performanceStatusText;

    // Diagnostics State
    public string ActiveRuntime { get; private set; } = "Unknown";
    public bool IsSimulatorActive { get; private set; } = false;
    public bool IsHmdConnected { get; private set; } = false;
    public bool IsLeftTracked { get; private set; } = false;
    public bool IsRightTracked { get; private set; } = false;
    public Vector3 HmdPosition { get; private set; }
    public Vector3 LeftControllerPos { get; private set; }
    public Vector3 RightControllerPos { get; private set; }
    public float LeftBattery { get; private set; } = -1f;
    public float RightBattery { get; private set; } = -1f;
    public string ActiveCameraName { get; private set; } = "None";
    public float CurrentFPS { get; private set; } = 0f;
    public float FrameDeltaTimeMs { get; private set; } = 0f;

    private float fpsUpdateTimer = 0f;
    private int frameCountSinceLastUpdate = 0;
    private GUIStyle panelBoxStyle;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;

    private void Start()
    {
        transform.position = worldSpacePosition;
        transform.rotation = Quaternion.Euler(worldSpaceRotation);
        UpdateRuntimeDiagnostics();
    }

    private void Update()
    {
        HandleToggleInput();
        UpdateFrameMetrics();
        UpdateTrackingState();
        UpdateRuntimeDiagnostics();
        UpdateWorldSpaceCanvas();
    }

    private void HandleToggleInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.xKey.wasPressedThisFrame)
        {
            showPanel = !showPanel;
        }
#else
        try
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                showPanel = !showPanel;
            }
        }
        catch { }
#endif

        // Also check VR controller secondary button (B/Y button)
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch) ||
            OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            showPanel = !showPanel;
        }
    }

    private void UpdateFrameMetrics()
    {
        fpsUpdateTimer += Time.unscaledDeltaTime;
        frameCountSinceLastUpdate++;

        if (fpsUpdateTimer >= 0.3f)
        {
            CurrentFPS = frameCountSinceLastUpdate / fpsUpdateTimer;
            FrameDeltaTimeMs = (fpsUpdateTimer / frameCountSinceLastUpdate) * 1000f;
            frameCountSinceLastUpdate = 0;
            fpsUpdateTimer = 0f;
        }
    }

    private void UpdateRuntimeDiagnostics()
    {
        // 1. Detect active camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (cams.Length > 0) cam = cams[0];
        }
        ActiveCameraName = cam != null ? cam.name : "None";

        // 2. Detect XR Runtime
        ActiveRuntime = XRSettings.loadedDeviceName;
        if (string.IsNullOrEmpty(ActiveRuntime) || ActiveRuntime == "None")
        {
            ActiveRuntime = OVRPlugin.version != null ? $"Meta OVRPlugin {OVRPlugin.version}" : "None (Desktop Fallback)";
        }

        // 3. Detect Simulator
        IsSimulatorActive = false;
        try
        {
            string simEnv = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON");
            if (!string.IsNullOrEmpty(simEnv) && simEnv.IndexOf("simulator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                IsSimulatorActive = true;
            }
#if UNITY_EDITOR
            if (OVRManager.isHmdPresent && !XRSettings.isDeviceActive)
            {
                IsSimulatorActive = true;
            }
#endif
        }
        catch { }
    }

    private void UpdateTrackingState()
    {
        // Query OVRManager
        IsHmdConnected = OVRManager.isHmdPresent;
        
        // HMD position
        if (IsHmdConnected)
        {
            var centerCam = Camera.main;
            if (centerCam != null)
            {
                HmdPosition = centerCam.transform.position;
            }
        }

        // Controllers via OVRInput
        IsLeftTracked = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);
        IsRightTracked = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);

        LeftControllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        RightControllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

        try
        {
            LeftBattery = OVRInput.GetControllerBatteryPercentRemaining(OVRInput.Controller.LTouch);
            RightBattery = OVRInput.GetControllerBatteryPercentRemaining(OVRInput.Controller.RTouch);
        }
        catch
        {
            LeftBattery = -1f;
            RightBattery = -1f;
        }

        // Subsystem validation fallback via UnityEngine.XR
        if (!IsHmdConnected)
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count > 0 && devices[0].isValid)
            {
                IsHmdConnected = true;
                if (devices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 hmdPos))
                {
                    HmdPosition = hmdPos;
                }
            }
        }
    }

    private void UpdateWorldSpaceCanvas()
    {
        if (headerText != null)
        {
            headerText.text = "<b>Meta XR & Hardware Telemetry</b>";
        }
        if (runtimeStatusText != null)
        {
            runtimeStatusText.text = $"Runtime: {ActiveRuntime}\n" +
                                     $"Simulator Active: {(IsSimulatorActive ? "YES (OpenXR Simulator)" : "NO")}\n" +
                                     $"Platform: {Application.platform}";
        }
        if (hmdStatusText != null)
        {
            hmdStatusText.text = $"HMD Connected: {(IsHmdConnected ? "TRUE" : "FALSE")}\n" +
                                 $"HMD Pos: ({HmdPosition.x:F2}, {HmdPosition.y:F2}, {HmdPosition.z:F2})\n" +
                                 $"Active Camera: {ActiveCameraName}";
        }
        if (controllerStatusText != null)
        {
            string lStr = IsLeftTracked ? $"Tracked ({LeftControllerPos.x:F2}, {LeftControllerPos.y:F2}) Bat:{LeftBattery}%" : "Searching/Idle";
            string rStr = IsRightTracked ? $"Tracked ({RightControllerPos.x:F2}, {RightControllerPos.y:F2}) Bat:{RightBattery}%" : "Searching/Idle";
            controllerStatusText.text = $"L Touch: {lStr}\n" +
                                        $"R Touch: {rStr}";
        }
        if (performanceStatusText != null)
        {
            performanceStatusText.text = $"FPS: {CurrentFPS:F0} ({FrameDeltaTimeMs:F1} ms)";
        }
    }

    private void OnGUI()
    {
        if (!showPanel || Application.isMobilePlatform) return;

        InitStyles();

        GUILayout.BeginArea(new Rect(Screen.width - 340, 15, 325, 320), panelBoxStyle);
        GUILayout.Label("<b>XR DIAGNOSTICS & TRACKING</b>", headerStyle);
        GUILayout.Space(4);

        GUILayout.Label($"<b>Platform:</b> {Application.platform}");
        GUILayout.Label($"<b>XR Runtime:</b> {ActiveRuntime}");
        string simStr = IsSimulatorActive ? "<color=#00ff88>ACTIVE (Meta XR Simulator 2.0)</color>" : "<color=#cccccc>INACTIVE / HARDWARE</color>";
        GUILayout.Label($"<b>Meta XR Simulator:</b> {simStr}");
        GUILayout.Label($"<b>Active Camera:</b> {ActiveCameraName}");

        GUILayout.Space(6);
        string hmdStr = IsHmdConnected ? "<color=#00ff88>TRACKED</color>" : "<color=#ffaa00>NO HMD / DESKTOP</color>";
        GUILayout.Label($"<b>HMD State:</b> {hmdStr}");
        if (IsHmdConnected)
        {
            GUILayout.Label($"  Pos: ({HmdPosition.x:F2}, {HmdPosition.y:F2}, {HmdPosition.z:F2})");
        }

        GUILayout.Space(4);
        string lStr = IsLeftTracked ? "<color=#00ff88>Connected</color>" : "<color=#888888>Idle</color>";
        string rStr = IsRightTracked ? "<color=#00ff88>Connected</color>" : "<color=#888888>Idle</color>";
        GUILayout.Label($"<b>L Controller:</b> {lStr} {(LeftBattery >= 0 ? $"({LeftBattery}%)" : "")}");
        GUILayout.Label($"<b>R Controller:</b> {rStr} {(RightBattery >= 0 ? $"({RightBattery}%)" : "")}");

        GUILayout.Space(6);
        string fpsColor = CurrentFPS >= 60f ? "#00ff88" : (CurrentFPS >= 45f ? "#ffbb00" : "#ff4444");
        GUILayout.Label($"<b>Rendering:</b> <color={fpsColor}>{CurrentFPS:F0} FPS</color> ({FrameDeltaTimeMs:F1} ms)");

        GUILayout.Space(4);
        GUILayout.Label("<size=10>Press X or Touch B/Y Button to Toggle</size>");
        GUILayout.EndArea();
    }

    private void InitStyles()
    {
        if (panelBoxStyle == null)
        {
            panelBoxStyle = new GUIStyle(GUI.skin.box);
            panelBoxStyle.padding = new RectOffset(10, 10, 8, 8);
        }
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.richText = true;
            headerStyle.fontSize = 12;
            headerStyle.fontStyle = FontStyle.Bold;
        }
        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.richText = true;
            bodyStyle.fontSize = 11;
        }
    }
}
