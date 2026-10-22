// CrossPlatformInputController.cs - Desktop First-Person Navigation & Unified Input
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Fully compatible with Unity New Input System (UnityEngine.InputSystem) and Legacy Fallback
// Dynamically moves active rig (Meta_OVRCameraRig or Desktop_FPS_Rig)

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class CrossPlatformInputController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float lookSensitivity = 0.15f;
    public float verticalLookLimit = 80.0f;

    [Header("Room Collision Bounds (Meters)")]
    public float minX = -2.2f;
    public float maxX = 2.2f;
    public float minZ = -3.2f;
    public float maxZ = 3.2f;

    [Header("Transform Targets")]
    public Transform targetRigTransform;
    public Transform playerCamera;

    [Header("Coupled Subsystems")]
    public EnvironmentModifier environmentModifier;
    public MovieScreenPlayer moviePlayer;
    public AcousticRaycaster raycaster;
    public RT60TelemetryLogger telemetryLogger;
    public PlatformManager platformManager;

    private float pitch = 0.0f;
    private float yaw = 0.0f;
    private bool isMouseLookActive = false;

    private void Awake()
    {
        FindCoupledSubsystems();
    }

    private void Start()
    {
        UpdateActiveTargets();
        if (playerCamera != null)
        {
            Vector3 currentRot = playerCamera.eulerAngles;
            yaw = currentRot.y;
            pitch = currentRot.x;
        }
    }

    private void Update()
    {
        UpdateActiveTargets();
        HandleKeyboardMovement();
        HandleMouseLook();
        HandleHotkeys();
    }

    private void FindCoupledSubsystems()
    {
        if (environmentModifier == null) environmentModifier = FindAnyObjectByType<EnvironmentModifier>();
        if (moviePlayer == null) moviePlayer = FindAnyObjectByType<MovieScreenPlayer>();
        if (raycaster == null) raycaster = FindAnyObjectByType<AcousticRaycaster>();
        if (telemetryLogger == null) telemetryLogger = FindAnyObjectByType<RT60TelemetryLogger>();
        if (platformManager == null) platformManager = FindAnyObjectByType<PlatformManager>();
    }

    private void UpdateActiveTargets()
    {
        if (platformManager == null) platformManager = FindAnyObjectByType<PlatformManager>();

        if (targetRigTransform == null || !targetRigTransform.gameObject.activeInHierarchy)
        {
            if (platformManager != null)
            {
                if (platformManager.activePlatformMode == PlatformManager.PlatformMode.WindowsDesktop && platformManager.desktopRig != null && platformManager.desktopRig.activeInHierarchy)
                {
                    targetRigTransform = platformManager.desktopRig.transform;
                }
                else if (platformManager.metaXRRig != null && platformManager.metaXRRig.activeInHierarchy)
                {
                    targetRigTransform = platformManager.metaXRRig.transform;
                }
            }
            if (targetRigTransform == null) targetRigTransform = transform;
        }

        if (playerCamera == null || !playerCamera.gameObject.activeInHierarchy)
        {
            Camera cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }
    }

    private void HandleKeyboardMovement()
    {
        if (targetRigTransform == null) return;

        float h = 0f;
        float v = 0f;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        }
        else
#endif
        {
            try
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }
            catch { /* Ignore if legacy input is disabled */ }
        }

        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            Transform refTransform = playerCamera != null ? playerCamera : targetRigTransform;
            Vector3 forward = refTransform.forward;
            Vector3 right = refTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = (forward * v + right * h).normalized;
            Vector3 newPos = targetRigTransform.position + moveDir * (moveSpeed * Time.deltaTime);

            // Clamp within room walls
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);

            targetRigTransform.position = newPos;
        }
    }

    private void HandleMouseLook()
    {
        // Only run mouse look in desktop mode; Meta XR Simulator manages its own look orientation
        if (platformManager != null && platformManager.activePlatformMode != PlatformManager.PlatformMode.WindowsDesktop)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.wasPressedThisFrame)
            {
                isMouseLookActive = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (mouse.rightButton.wasReleasedThisFrame)
            {
                isMouseLookActive = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (isMouseLookActive && targetRigTransform != null)
            {
                Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
                yaw += delta.x;
                pitch -= delta.y;
                pitch = Mathf.Clamp(pitch, -verticalLookLimit, verticalLookLimit);

                targetRigTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (playerCamera != null && playerCamera != targetRigTransform)
                {
                    playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }
            return;
        }
#endif

        try
        {
            if (Input.GetMouseButtonDown(1))
            {
                isMouseLookActive = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isMouseLookActive = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (isMouseLookActive && targetRigTransform != null)
            {
                float mouseX = Input.GetAxis("Mouse X") * (lookSensitivity * 10f);
                float mouseY = Input.GetAxis("Mouse Y") * (lookSensitivity * 10f);

                yaw += mouseX;
                pitch -= mouseY;
                pitch = Mathf.Clamp(pitch, -verticalLookLimit, verticalLookLimit);

                targetRigTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (playerCamera != null && playerCamera != targetRigTransform)
                {
                    playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }
        }
        catch { /* Ignore legacy input disabled */ }
    }

    private void HandleHotkeys()
    {
        bool key1 = false, key2 = false, key3 = false, keySpace = false, keyM = false, keyR = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            key1 = kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
            key2 = kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;
            key3 = kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame;
            keySpace = kb.spaceKey.wasPressedThisFrame;
            keyM = kb.mKey.wasPressedThisFrame;
            keyR = kb.rKey.wasPressedThisFrame;
        }
        else
#endif
        {
            try
            {
                key1 = Input.GetKeyDown(KeyCode.Alpha1);
                key2 = Input.GetKeyDown(KeyCode.Alpha2);
                key3 = Input.GetKeyDown(KeyCode.Alpha3);
                keySpace = Input.GetKeyDown(KeyCode.Space);
                keyM = Input.GetKeyDown(KeyCode.M);
                keyR = Input.GetKeyDown(KeyCode.R);
            }
            catch { /* Ignore legacy exceptions */ }
        }

        // Scenario 1: Bare Room
        if (key1)
        {
            if (environmentModifier != null) environmentModifier.ApplyScenario(EnvironmentModifier.ScenarioType.BaselineBare);
        }
        // Scenario 2: Over-treated
        else if (key2)
        {
            if (environmentModifier != null) environmentModifier.ApplyScenario(EnvironmentModifier.ScenarioType.OverTreated);
        }
        // Scenario 3: Optimized
        else if (key3)
        {
            if (environmentModifier != null) environmentModifier.ApplyScenario(EnvironmentModifier.ScenarioType.Optimized);
        }

        // Toggle Movie Play/Pause
        if (keySpace)
        {
            if (moviePlayer != null)
            {
                if (moviePlayer.IsPlaying && !moviePlayer.IsPaused) moviePlayer.PauseMovie();
                else if (moviePlayer.IsPaused) moviePlayer.ResumeMovie();
                else moviePlayer.PlayMovie();
            }
        }

        // Run Measurement
        if (keyM)
        {
            if (raycaster != null) raycaster.RunSimulation();
        }

        // Reset
        if (keyR)
        {
            if (environmentModifier != null) environmentModifier.ApplyScenario(EnvironmentModifier.ScenarioType.BaselineBare);
            if (moviePlayer != null) moviePlayer.RestartMovie();
        }
    }
}
