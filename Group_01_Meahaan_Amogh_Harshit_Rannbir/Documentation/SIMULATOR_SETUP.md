# IVRAR Group 01 — Meta XR Simulator Setup & Workflow Guide

## 1. Meta XR Simulator Overview

The **Meta XR Simulator 2.0** allows end-to-end testing of Meta Quest 2 / Quest 3 / Quest Pro VR applications directly inside the Unity Editor without tethering a physical headset. It simulates:
- Headset 6DoF position and orientation tracking.
- Left and Right Touch controller 6DoF tracking, buttons, thumbsticks, index triggers, and hand grips.
- Hand tracking synthesis and gesture simulation.
- Compositor output and eye rendering.

---

## 2. Environment Configuration & OpenXR Registration

The Meta XR Simulator is driven by an OpenXR API layer and registered via the `XR_RUNTIME_JSON` environment variable:

```text
XR_RUNTIME_JSON = C:\Program Files\MetaXRSimulator\v207.1\meta_openxr_simulator.json
```

### Verification in Unity
To confirm the simulator is active in Unity:
1. Open **Meta > Meta XR Simulator** in the Unity top menu bar.
2. Ensure the status reads **"Meta XR Simulator is activated"**.
3. Under **Edit > Project Settings > XR Plug-in Management**:
   - For **Standalone (Windows)**: Ensure **OpenXR** is checked.
   - Under **OpenXR Features**: Ensure **Meta XR Feature** is enabled.

---

## 3. Simulator Keyboard & Mouse Interaction Controls

When running in PlayMode with Meta XR Simulator active, the simulator window accepts the following input controls:

### Headset (HMD) Navigation
| Action | Key / Mouse Binding |
| :--- | :--- |
| **HMD Pitch & Yaw (Look)** | Hold **Right Mouse Button** and move mouse |
| **HMD Position (Translate)** | Hold **Right Mouse Button** + **W / A / S / D** |
| **HMD Vertical Height** | Hold **Right Mouse Button** + **E** (Up) / **Q** (Down) |
| **Reset HMD Pose** | **Ctrl + R** inside simulator window |

### Left Touch Controller Manipulation
| Action | Key / Mouse Binding |
| :--- | :--- |
| **Activate Left Controller Control** | Hold **Left Shift** |
| **Rotate Left Controller** | Hold **Left Shift** + Move Mouse |
| **Translate Left Controller** | Hold **Left Shift** + **W / A / S / D** |
| **Left Index Trigger** | Hold **Left Shift** + **Left Mouse Button** |
| **Left Hand Grip (Grab)** | Hold **Left Shift** + **G** |
| **Primary Button (X Button)** | Hold **Left Shift** + **X** |
| **Secondary Button (Y Button)** | Hold **Left Shift** + **Y** |

### Right Touch Controller Manipulation
| Action | Key / Mouse Binding |
| :--- | :--- |
| **Activate Right Controller Control** | Hold **Spacebar** (or Right Shift) |
| **Rotate Right Controller** | Hold **Spacebar** + Move Mouse |
| **Translate Right Controller** | Hold **Spacebar** + **W / A / S / D** |
| **Right Index Trigger** | Hold **Spacebar** + **Left Mouse Button** |
| **Right Hand Grip (Grab Subwoofer)** | Hold **Spacebar** + **G** |
| **Primary Button (A Button)** | Hold **Spacebar** + **A** |
| **Secondary Button (B Button)** | Hold **Spacebar** + **B** |

---

## 4. Simulator Interactive Workflows in Home Cinema

### Grabbing and Moving the Subwoofer
1. Hold **Spacebar** to bring the right Touch controller forward into view.
2. Aim the controller towards `Subwoofer_LFE` on the floor.
3. Hold **G** (Hand Grip) or **Left Mouse Button** (Index Trigger).
4. Move the mouse to drag the subwoofer across the cinema floor between the front corner modal node and the 1/3 axial position.
5. Notice the live acoustic SPL balance change on the `XRDiagnosticsPanel` and the desktop dashboard.
6. Release **G** to drop the subwoofer. The subwoofer automatically stays clamped to $y = 0.25\text{ m}$ and within room boundaries.

### Toggling Comparative Acoustic Scenarios
- Press **1** on the keyboard for **Baseline Bare** room ($RT_{60} = 1.42\text{ s}$).
- Press **2** on the keyboard for **Over-Treated** room ($RT_{60} = 0.18\text{ s}$).
- Press **3** on the keyboard for **Optimized** room ($RT_{60} = 0.45\text{ s}$, CEDIA compliant).

### Toggling Diagnostics Overlays
- Press **X** or Touch controller **B / Y button** to toggle the `XRDiagnosticsPanel`.
- Press **Tab** to toggle the Desktop HUD overlay.

---

## 5. Troubleshooting: Black Screen in Meta XR Simulator

### Root Cause Analysis
If the Meta XR Simulator window is black upon pressing Play, the root cause is almost always **XR Camera Deactivation**:
1. During initial scene awake, OpenXR and the XR Display Subsystem undergo an asynchronous handshake. If the platform manager queries `XRSettings.isDeviceActive` synchronously during `Awake()`, it falsely detects desktop mode and deactivates `Meta_OVRCameraRig` via `SetActive(false)`.
2. Because `Meta_OVRCameraRig` is deactivated, its `CenterEyeAnchor` camera ceases rendering.
3. OpenXR / Meta XR Simulator receives no submitted eye buffers, resulting in a black compositor window.

### Permanent Resolution Applied
- [`PlatformManager.cs`](file:///D:/NMIMS-MPSTME-IVRAR-2026/Group_01_Meahaan_Amogh_Harshit_Rannbir/Assets/_Project/Scripts/Platform/PlatformManager.cs) now defaults to `PlatformMode.WindowsPCVR` when OpenXR or Meta XR Simulator is present, ensuring `Meta_OVRCameraRig` and `CenterEyeAnchor` are active from the very first frame.
- `CenterEyeAnchor` camera is enabled with `StereoTargetEyeMask.Both` and tagged `MainCamera`.
- `Desktop_Camera` is configured with `StereoTargetEyeMask.None` and disabled while in VR mode to prevent rendering conflicts.
- Press **V** or **F1** at any time to toggle dynamically between Desktop FPS and VR Simulator mode.

---

## 6. Expected Simulator Warnings (Non-Critical)

During simulator initialization, the OpenXR runtime logs several informational capability notices:
- `xrGetInstanceProcAddr failed to get function pointer for 'xrGetSpaceTriangleMeshMETA'`: Expected because mesh reconstruction is only available on physical Quest headsets with MR depth cameras.
- `Local Dimming feature is not supported`: Expected because local dimming requires Quest Pro mini-LED backlights.
- `Unsupported path: /user/hand/left/input/trigger/proximity_fb`: Expected because touch proximity capacitive sensors are simulated digitally.

These warnings are benign and do not impede simulator tracking, rendering, audio, or interaction.
