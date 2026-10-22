// PlatformManager.cs - Unified Hardware Platform Detection & Diagnostics
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Automatically initializes Meta_OVRCameraRig for Meta XR Simulator / OpenXR / Quest

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class PlatformManager : MonoBehaviour
{
    public enum PlatformMode
    {
        WindowsDesktop,
        WindowsPCVR,
        MetaQuestStandalone
    }

    public enum PlatformPreference
    {
        AutoDetect,
        ForceVR,
        ForceDesktop
    }

    [Header("Platform Mode State")]
    public PlatformPreference platformPreference = PlatformPreference.AutoDetect;
    public PlatformMode activePlatformMode = PlatformMode.WindowsPCVR;
    public bool isXRActive = false;
    public bool isHeadsetTracked = false;
    public bool isLeftControllerTracked = false;
    public bool isRightControllerTracked = false;

    [Header("Rig Instances")]
    public GameObject desktopRig;
    public GameObject metaXRRig;

    [Header("Spawn Position (Primary Listening Position)")]
    public Vector3 spawnPosition = new Vector3(0.0f, 1.15f, -0.4f);
    public Vector3 spawnForward = Vector3.forward;

    // Diagnostics strings for UI display
    public string XRRuntimeName { get; private set; } = "None";
    public string ActiveCameraName { get; private set; } = "None";
    public string ControllerStatusMessage { get; private set; } = "Initializing...";

    private bool hasInitializedRigs = false;

    private void Awake()
    {
        FindRigsIfMissing();
        DetectPlatform();
    }

    private void Start()
    {
        InitializeRigs();
    }

    private void Update()
    {
        PollXRTrackingState();
        HandleRuntimeToggleInput();

        // If HMD tracking became active after startup in AutoDetect mode, guarantee VR rig is active
        if (!hasInitializedRigs || (isHeadsetTracked && activePlatformMode == PlatformMode.WindowsDesktop && platformPreference == PlatformPreference.AutoDetect))
        {
            activePlatformMode = PlatformMode.WindowsPCVR;
            InitializeRigs();
        }
    }

    public void FindRigsIfMissing()
    {
        if (metaXRRig == null)
        {
            metaXRRig = GameObject.Find("Meta_OVRCameraRig");
        }
        if (desktopRig == null)
        {
            desktopRig = GameObject.Find("Desktop_FPS_Rig");
        }
    }

    /// <summary>
    /// Detects current target runtime platform and XR subsystem presence.
    /// Prioritizes OpenXR / Meta XR Simulator in Editor and PC builds.
    /// </summary>
    public void DetectPlatform()
    {
        if (platformPreference == PlatformPreference.ForceVR)
        {
            activePlatformMode = (Application.platform == RuntimePlatform.Android) ? PlatformMode.MetaQuestStandalone : PlatformMode.WindowsPCVR;
            Debug.Log($"[PlatformManager] ForceVR enabled: {activePlatformMode}");
            return;
        }

        if (platformPreference == PlatformPreference.ForceDesktop)
        {
            activePlatformMode = PlatformMode.WindowsDesktop;
            Debug.Log($"[PlatformManager] ForceDesktop enabled: {activePlatformMode}");
            return;
        }

        // AutoDetect Logic:
        if (Application.platform == RuntimePlatform.Android)
        {
            activePlatformMode = PlatformMode.MetaQuestStandalone;
        }
        else if (IsOpenXREnabled() || IsSimulatorEnvironment() || IsXRDisplaySubsystemRunning() || OVRManager.isHmdPresent)
        {
            // Default to WindowsPCVR in Unity Editor / PC with OpenXR / Meta XR Simulator
            activePlatformMode = PlatformMode.WindowsPCVR;
        }
        else
        {
            activePlatformMode = PlatformMode.WindowsDesktop;
        }

        Debug.Log($"[PlatformManager] Detected Platform Mode: {activePlatformMode}");
    }

    private bool IsOpenXREnabled()
    {
        try
        {
            var generalSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
            if (generalSettings != null && generalSettings.Manager != null)
            {
                if (generalSettings.Manager.activeLoader != null) return true;
                if (generalSettings.InitManagerOnStart) return true;
            }
        }
        catch { }
        return false;
    }

    private bool IsSimulatorEnvironment()
    {
        try
        {
            string simEnv = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON");
            if (!string.IsNullOrEmpty(simEnv) && simEnv.IndexOf("simulator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        catch { }

#if UNITY_EDITOR
        // In Unity Editor, default to enabling the VR rig for Meta XR Simulator workflow
        return true;
#else
        return false;
#endif
    }

    private bool IsXRDisplaySubsystemRunning()
    {
        var displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displaySubsystems);
        foreach (var sub in displaySubsystems)
        {
            if (sub.running) return true;
        }
        return false;
    }

    /// <summary>
    /// Activates the designated rig and positions it safely at the primary listening position.
    /// </summary>
    public void InitializeRigs()
    {
        FindRigsIfMissing();
        bool useVR = (activePlatformMode == PlatformMode.MetaQuestStandalone || activePlatformMode == PlatformMode.WindowsPCVR);

        if (useVR && metaXRRig != null)
        {
            // 1. Deactivate Desktop Rig to prevent camera / AudioListener conflicts
            if (desktopRig != null)
            {
                var dCam = desktopRig.GetComponentInChildren<Camera>(true);
                if (dCam != null) dCam.enabled = false;
                var dListener = desktopRig.GetComponentInChildren<AudioListener>(true);
                if (dListener != null) dListener.enabled = false;
                desktopRig.SetActive(false);
            }

            // 2. Activate Meta XR Rig at floor level (OVRCameraRig FloorLevel tracking adds eye height)
            metaXRRig.SetActive(true);
            metaXRRig.transform.position = new Vector3(spawnPosition.x, 0.0f, spawnPosition.z);
            metaXRRig.transform.rotation = Quaternion.LookRotation(spawnForward);

            // 3. Ensure CenterEyeAnchor is active and configured for stereo HMD rendering
            Transform trackingSpace = metaXRRig.transform.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                Transform centerEye = trackingSpace.Find("CenterEyeAnchor");
                if (centerEye != null)
                {
                    Camera cam = centerEye.GetComponent<Camera>();
                    if (cam != null)
                    {
                        cam.enabled = true;
                        cam.stereoTargetEye = StereoTargetEyeMask.Both;
                        cam.tag = "MainCamera";
                    }
                    AudioListener al = centerEye.GetComponent<AudioListener>();
                    if (al != null) al.enabled = true;
                }
            }

            ActiveCameraName = "OVRCameraRig (Meta XR Simulator / HMD)";
            hasInitializedRigs = true;
            Debug.Log("[PlatformManager] Initialized Meta_OVRCameraRig for VR / Simulator rendering.");
        }
        else
        {
            // Desktop Mode
            if (metaXRRig != null)
            {
                metaXRRig.SetActive(false);
            }

            if (desktopRig != null)
            {
                desktopRig.SetActive(true);
                desktopRig.transform.position = spawnPosition;
                desktopRig.transform.rotation = Quaternion.LookRotation(spawnForward);

                var dCam = desktopRig.GetComponentInChildren<Camera>(true);
                if (dCam != null)
                {
                    dCam.enabled = true;
                    dCam.stereoTargetEye = StereoTargetEyeMask.None; // Monoscopic only
                    dCam.tag = "MainCamera";
                }
                var dListener = desktopRig.GetComponentInChildren<AudioListener>(true);
                if (dListener != null) dListener.enabled = true;

                ActiveCameraName = "Desktop Camera (FPS Controller)";
                hasInitializedRigs = true;
                Debug.Log("[PlatformManager] Initialized Desktop_FPS_Rig for monoscopic desktop rendering.");
            }
        }
    }

    /// <summary>
    /// Switches dynamically between VR and Desktop mode.
    /// </summary>
    public void ToggleRigMode()
    {
        activePlatformMode = (activePlatformMode == PlatformMode.WindowsDesktop)
            ? PlatformMode.WindowsPCVR
            : PlatformMode.WindowsDesktop;

        platformPreference = (activePlatformMode == PlatformMode.WindowsDesktop)
            ? PlatformPreference.ForceDesktop
            : PlatformPreference.ForceVR;

        InitializeRigs();
        Debug.Log($"[PlatformManager] Manually toggled platform mode to: {activePlatformMode}");
    }

    private void HandleRuntimeToggleInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.vKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame))
        {
            ToggleRigMode();
        }
#else
        try
        {
            if (Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.F1))
            {
                ToggleRigMode();
            }
        }
        catch { }
#endif
    }

    /// <summary>
    /// Polls connected XR input devices to report accurate device tracking diagnostics.
    /// </summary>
    public void PollXRTrackingState()
    {
        var inputDevices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevices(inputDevices);

        isHeadsetTracked = false;
        isLeftControllerTracked = false;
        isRightControllerTracked = false;

        foreach (var device in inputDevices)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
            {
                isHeadsetTracked = device.isValid;
                XRRuntimeName = device.name;
            }
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller))
            {
                isLeftControllerTracked = device.isValid;
            }
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller))
            {
                isRightControllerTracked = device.isValid;
            }
        }

        // Also check OVRManager as secondary source
        if (!isHeadsetTracked && OVRManager.isHmdPresent)
        {
            isHeadsetTracked = true;
            XRRuntimeName = "Meta OpenXR Simulator / HMD";
        }

        isXRActive = isHeadsetTracked || activePlatformMode != PlatformMode.WindowsDesktop;

        if (activePlatformMode == PlatformMode.WindowsDesktop)
        {
            ControllerStatusMessage = "Desktop Keyboard/Mouse (Press V for VR)";
        }
        else
        {
            ControllerStatusMessage = $"HMD: {(isHeadsetTracked ? "Connected" : "Idle")} | L: {(isLeftControllerTracked ? "Tracked" : "Searching")} | R: {(isRightControllerTracked ? "Tracked" : "Searching")}";
        }
    }
}
