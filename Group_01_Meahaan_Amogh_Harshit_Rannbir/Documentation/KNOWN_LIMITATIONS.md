# IVRAR Group 01 — Known Limitations & Technical Considerations

## 1. Meta XR Simulator vs. Physical Quest 2 Hardware

1. **Capacitive Touch Proximity Sensors:**
   - In physical Meta Quest 2 / Quest 3 Touch controllers, capacitive thumb rest and trigger sensors detect physical finger resting before button depression.
   - In **Meta XR Simulator 2.0**, proximity is simulated digitally upon key depression. OpenXR logs benign informational warnings (`/user/hand/left/input/trigger/proximity_fb`) that do not impact grab or click logic.
2. **Local Dimming & Passthrough Sensors:**
   - Meta XR Simulator reports `Local Dimming feature is not supported` because local dimming is a hardware-specific feature of the Meta Quest Pro mini-LED display panels.
   - Passthrough room mesh generation calls (`xrGetSpaceRoomMeshMETA`) are stubbed in the simulator because RGB stereo passthrough cameras are only present on physical headset hardware.

---

## 2. Acoustic Raycasting Performance Budget

1. **Ray Density & Bounce Limits:**
   - The current impulse response generation uses a spherical Fibonacci lattice of **250 primary rays** with up to **30 specular reflections** per ray ($7,500$ ray segments maximum per acoustic measurement).
   - This executes synchronously in $< 4\text{ ms}$ on Windows Desktop/PCVR.
   - For standalone Meta Quest 2 (Snapdragon XR2 Gen 1), acoustic raycasting should be triggered on demand (e.g. when pressing **M** or repositioning the subwoofer) rather than continuously every frame to preserve the 72 Hz / 90 Hz render budget.
2. **Phase Interference & Wave Acoustics (Below Schroeder Frequency):**
   - Geometrical acoustics (raycasting, Sabine, Eyring) accurately models high- and mid-frequency specular reflections and diffuse reverberation.
   - Room modes below the Schroeder frequency ($f_s \approx 2000 \sqrt{\frac{T_{60}}{V}} \approx 130\text{ Hz}$) are approximated in this simulator using empirical axial standing wave calculations in `SubwooferController` and `VirtualSPLMeter`. Finite Element Modeling (FEM) / Boundary Element Modeling (BEM) would be required for sub-hertz modal wave solvers.

---

## 3. Video Decoding & Texture Fallback

1. **MovieScreenPlayer Procedural Fallback:**
   - If an MP4 video clip is missing or codec-unsupported on a target platform, `MovieScreenPlayer` automatically switches to a procedural SMPTE color bar generator and synthetic audio test tones (440 Hz / 1 kHz), ensuring the cinema screen and audio spatializers are always visually and sonically verifiable.
2. **Standalone Quest Video Codecs:**
   - Physical Meta Quest 2 hardware requires H.264 / HEVC video files encoded with baseline or main profile audio (AAC-LC). High-bitrate ProRes or MKV containers must be transcoded before inclusion in `Assets/_Project/Audio/MovieClips/`.

---

## 4. Platform Build Toolchain Requirements

1. **Windows PCVR & Desktop:**
   - Compiles and runs out-of-the-box using the installed Unity 6 LTS engine and DirectX 11/12 runtime.
2. **Meta Quest 2 Standalone Deployment:**
   - Requires Unity Android Build Support with Android SDK Platform 34+, NDK r25+, and OpenJDK 17.
   - Building the standalone `.apk` is configured in `Assets/Settings/Build Profiles/Meta Quest.asset` and executable via `IVRAR > Build Meta Quest Standalone APK` once the Android toolchain paths are registered in Unity Preferences.
