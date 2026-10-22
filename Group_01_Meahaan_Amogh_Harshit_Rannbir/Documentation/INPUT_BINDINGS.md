# IVRAR Group 01 — Unified Input Bindings & Control Specification

## 1. Overview

The **IVRAR Group 01** home theatre simulator employs a dual input mapping scheme:
- **Desktop First-Person Mode:** Seamless keyboard and mouse navigation with IMGUI/Canvas HUD.
- **VR / Meta Quest Mode:** Full 6DoF Touch controller integration with gesture grabbing, ray pointing, and UI selection.

Both modalities execute concurrently without conflicting axes or duplicate camera rigs.

---

## 2. Desktop Mode Input Bindings

| Control / Input | Subsystem / Component | Functionality |
| :--- | :--- | :--- |
| **W / A / S / D** or **Arrow Keys** | `CrossPlatformInputController` | First-person translation at seated eye level ($y = 1.15\text{ m}$), clamped to room bounds ($x \in [-2.2, 2.2]\text{ m}$, $z \in [-3.2, 3.2]\text{ m}$). |
| **Right Mouse Button (Hold & Drag)** | `CrossPlatformInputController` | First-person camera look (Yaw & Pitch) with clamped vertical angle ($\pm 80^\circ$). Cursor is locked while held and freed when released. |
| **Left Mouse Button (Click & Drag on Subwoofer)** | `SubwooferController` | Direct manipulation of the subwoofer across the cinema floor plane ($y = 0.25\text{ m}$). Clamped to interior walls. |
| **Key '1' / Numpad '1'** | `EnvironmentModifier` | Applies **Scenario A: Baseline Bare Room** ($RT_{60} = 1.42\text{ s}$, untreated drywall/concrete/glass, high reverberation). |
| **Key '2' / Numpad '2'** | `EnvironmentModifier` | Applies **Scenario B: Over-Treated Room** ($RT_{60} = 0.18\text{ s}$, 100% polyurethane acoustic foam, acoustically dead). |
| **Key '3' / Numpad '3'** | `EnvironmentModifier` | Applies **Scenario C: Optimized Room** ($RT_{60} = 0.45\text{ s}$, hybrid broadband absorption + 2D QRD diffusers, CEDIA RP22 compliant). |
| **Spacebar** | `MovieScreenPlayer` | Toggles movie playback between Play and Pause. Plays procedural SMPTE pattern and test tones if video is offline. |
| **Key 'M'** | `AcousticRaycaster` | Fires real-time spherical Fibonacci lattice acoustic raycasts (250 rays, 30 bounces) and calculates Schroeder decay. |
| **Key 'R'** | Simulation Services | Resets cinema room to baseline bare configuration and restarts video playback. |
| **Tab / Key 'H'** | `CinemaUIDashboard` | Toggles the desktop IMGUI HUD telemetry overlay. |
| **Key 'X'** | `XRDiagnosticsPanel` | Toggles the real-time XR tracking and telemetry diagnostics overlay. |

---

## 3. VR / Meta Quest Touch Controller Bindings

| Physical Touch Control | Hand | `OVRInput` Target | Functionality |
| :--- | :---: | :--- | :--- |
| **Left Thumbstick** | Left | `OVRInput.Axis2D.PrimaryThumbstick` | Continuous smooth locomotion within the cinema room. |
| **Right Thumbstick** | Right | `OVRInput.Axis2D.SecondaryThumbstick` | Snap-turn ($30^\circ$ increments) or smooth rotation. |
| **Hand Grip Trigger** | Either | `OVRInput.Button.PrimaryHandTrigger` | Proximity grab of `Subwoofer_LFE` to slide across floor. |
| **Index Trigger** | Right | `OVRInput.Button.PrimaryIndexTrigger` | Remote ray pointer aiming and UI button selection. |
| **Button A** | Right | `OVRInput.Button.One` | Confirm / Run acoustic measurement. |
| **Button B** | Right | `OVRInput.Button.Two` | Toggle `XRDiagnosticsPanel` floating panel. |
| **Button X** | Left | `OVRInput.Button.Three` | Cycle through acoustic scenarios (Bare $\rightarrow$ Over-treated $\rightarrow$ Optimized). |
| **Button Y** | Left | `OVRInput.Button.Four` | Toggle movie playback (Play / Pause). |
| **Menu Button** | Left | `OVRInput.Button.Start` | Toggle world-space cinema UI dashboard. |

---

## 4. Input System Configuration Details

- **Package:** `com.unity.inputsystem` v1.20.0
- **Player Settings:** `activeInputHandler: 2` (Both)
- **Defensive Safeguards:** All script-level input access checks for `Keyboard.current != null` and `Mouse.current != null` before reading values, preventing headless server or pure VR runtime crashes.
