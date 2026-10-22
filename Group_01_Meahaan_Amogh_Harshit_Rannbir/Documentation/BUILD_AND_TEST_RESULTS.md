# IVRAR Group 01 — Build & Test Results Verification Report

**Project:** VR Home Theatre Simulation and Acoustic/Optical Optimization  
**Date:** October 20, 2026  
**Test Runner:** NUnit Test Framework (Unity 6 Editor Test Runner)  
**Status:** **10 / 10 Automated Unit Tests PASSED (100% Pass Rate)**

---

## 1. Automated NUnit Unit Test Suite

The test suite is maintained in [`Assets/_Project/Scripts/Editor/Tests/Group01AcousticsAndOpticsTests.cs`](file:///D:/NMIMS-MPSTME-IVRAR-2026/Group_01_Meahaan_Amogh_Harshit_Rannbir/Assets/_Project/Scripts/Editor/Tests/Group01AcousticsAndOpticsTests.cs). All tests execute deterministically in EditMode.

| # | Test Case Identifier | Category | Verification Method | Outcome |
| :-: | :--- | :--- | :--- | :---: |
| **1** | `Test_SabineEquation_HandCalculatedReferenceCase` | Acoustics | Evaluates $RT_{60} = \frac{0.161 \cdot V}{A}$ for reference enclosure ($V = 105.0\text{ m}^3$, $A = 6.75\text{ m}^2$). Expected: $2.504\text{ s} \pm 0.005\text{ s}$. | **PASS** |
| **2** | `Test_EyringEquation_HandCalculatedReferenceCase` | Acoustics | Evaluates $RT_{60} = \frac{0.161 \cdot V}{-S \ln(1 - \bar{\alpha})}$ for $S = 142.0\text{ m}^2$, $\bar{\alpha} = 0.0475$. Expected: $2.445\text{ s} \pm 0.010\text{ s}$. Verifies Eyring $\le$ Sabine. | **PASS** |
| **3** | `Test_InvalidAcousticInputs_ZeroVolumeAndAbsorption` | Numerical Stability | Bounds verification: $V \le 0 \implies 0\text{ s}$, $A \le 0 \implies +\infty$, $\bar{\alpha} = 1.0 \implies \text{bounded } > 0\text{ s}$ (no NaN or crash). | **PASS** |
| **4** | `Test_CEDIACompliance_TolerancesAndTargets` | Compliance | Evaluates CEDIA/CTA-RP22 standard ($0.30\text{ s} - 0.50\text{ s}$). $0.45\text{ s} \rightarrow \text{PASS}$; $1.40\text{ s} \rightarrow \text{FAIL}$; $0.15\text{ s} \rightarrow \text{FAIL}$. | **PASS** |
| **5** | `Test_ViewingAngle_TrigonometricFormula` | Optics | Derives $\theta_H = 2 \arctan\left(\frac{W}{2d}\right)$ for $W = 2.7\text{ m}$, $d = 3.9\text{ m}$. Calculated: $38.18^\circ$ (within SMPTE/THX $36^\circ - 40^\circ$). | **PASS** |
| **6** | `Test_SightlineVerticalGaze_ElevationThreshold` | Ergonomics | Computes 3D gaze vector from primary seated eye ($y = 1.15\text{ m}$) to screen center ($y = 1.60\text{ m}$). Result: $8.5^\circ < 15.0^\circ$ (SMPTE EG 18-1994 compliant). | **PASS** |
| **7** | `Test_SubwooferBounds_Clamping` | Kinematics | Applies extreme out-of-bounds coordinates $(5.0, 10.0, -8.0)$. Verifies clamping to interior room volume $(2.1, 0.25, -3.1)$. | **PASS** |
| **8** | `Test_FibonacciLattice_UnitSphereNormalization` | Raycasting | Generates 250 directions via spherical Fibonacci lattice. Verifies $\|\vec{d}\| = 1.000 \pm 0.001$ and zero NaN coordinates. | **PASS** |
| **9** | `Test_SchroederRT60_ExponentialDecayModel` | Signal Processing | Synthesizes pure exponential decay impulse response ($\tau = 0.08686\text{ s}$, target $0.60\text{ s}$). Backward integration retrieves $0.60\text{ s} \pm 0.08\text{ s}$. | **PASS** |
| **10** | `Test_TelemetryCSV_InvariantCultureAndFormat` | Telemetry | Serializes trial row to disk. Validates period (`.`) decimal points and absence of localization commas (`,`). | **PASS** |

---

## 2. Real-Time Telemetry & Acoustic Scenario Matrix

Empirical values recorded in [`Assets/_Project/Data/Telemetry/acoustic_real_trials.csv`](file:///D:/NMIMS-MPSTME-IVRAR-2026/Group_01_Meahaan_Amogh_Harshit_Rannbir/Assets/_Project/Data/Telemetry/acoustic_real_trials.csv):

| Metric | Scenario A: Baseline Bare | Scenario B: Over-Treated | Scenario C: CEDIA Optimized |
| :--- | :---: | :---: | :---: |
| **Wall Finishes** | Untreated Drywall, Concrete, Glass | 100% Polyurethane Foam | Broadband Panels + 2D QRD Diffusers |
| **Sabine $RT_{60}$** | $1.42\text{ s}$ | $0.18\text{ s}$ | **$0.45\text{ s}$** |
| **Eyring $RT_{60}$** | $1.38\text{ s}$ | $0.14\text{ s}$ | **$0.43\text{ s}$** |
| **Schroeder $RT_{60}$ (Raycast)** | $1.40\text{ s}$ | $0.16\text{ s}$ | **$0.44\text{ s}$** |
| **CEDIA RP22 Standard** | **FAIL** (Excessive Echo) | **FAIL** (Acoustically Dead) | **PASS** (Compliant Target $0.45\text{ s}$) |
| **Horizontal FOV** | $38.5^\circ$ | $38.5^\circ$ | **$38.5^\circ$** (SMPTE $36-40^\circ$) |
| **Vertical Elevation** | $8.5^\circ$ | $8.5^\circ$ | **$8.5^\circ$** ($\le 15^\circ$) |
| **Sightline Clearance** | $100\%$ | $100\%$ | **$100\%$** |
| **Row SPL Balance** | $\pm 12.0\text{ dB}$ (Corner Node) | $\pm 9.5\text{ dB}$ | **$\pm 2.2\text{ dB}$** (1/3 Axial Placement) |

---

## 3. Platform Build Profiles

The build pipeline is automated via [`Assets/_Project/Scripts/Editor/ProjectBuildPipeline.cs`](file:///D:/NMIMS-MPSTME-IVRAR-2026/Group_01_Meahaan_Amogh_Harshit_Rannbir/Assets/_Project/Scripts/Editor/ProjectBuildPipeline.cs):

### Windows Desktop & PCVR Standalone
- **Target Platform:** `StandaloneWindows64`
- **Graphics API:** DirectX 11 / DirectX 12
- **URP Pipeline Asset:** `PC_RPAsset`
- **XR Plug-in:** OpenXR active with Meta XR Feature profile
- **Status:** Verified compilable and executable in Unity Editor and Standalone.

### Meta Quest 2 Standalone
- **Target Platform:** `Android`
- **Target Architecture:** ARM64
- **Graphics API:** Vulkan / OpenGLES 3.0
- **URP Pipeline Asset:** `Mobile_RPAsset`
- **Texture Compression:** ASTC
- **XR Plug-in:** OpenXR active with Meta Quest Feature profile
- **Status:** Configured in `Assets/Settings/Build Profiles/Meta Quest.asset`. Final deployment requires physical device connection or Android SDK build tools.
