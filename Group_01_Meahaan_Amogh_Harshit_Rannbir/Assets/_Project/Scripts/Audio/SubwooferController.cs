// SubwooferController.cs - Interactive Subwoofer Repositioning & Acoustic Modal Placement
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Supports Desktop Mouse Dragging and VR Controller (OVRInput) Grabbing

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class SubwooferController : MonoBehaviour
{
    [Header("Room Movement Bounds (Meters)")]
    public float minX = -2.10f;
    public float maxX = 2.10f;
    public float minZ = -3.10f;
    public float maxZ = 3.10f;
    public float floorY = 0.25f;

    [Header("Preset Placement Coordinates")]
    public Vector3 baselineCornerPos = new Vector3(2.10f, 0.25f, 3.0f);   // Front-Right Corner (Modal Node & Boom)
    public Vector3 optimizedPlacementPos = new Vector3(0.0f, 0.25f, 1.20f); // 1/3 Axial Distance (Smooth Response)

    [Header("Acoustic Audio Source")]
    public AudioSource bassAudioSource;
    public VirtualSPLMeter splMeter;

    [Header("VR Interaction Settings")]
    public float vrGrabDistance = 1.2f;

    // State
    public bool IsBeingDragged { get; private set; } = false;
    public bool IsVRGrabbed { get; private set; } = false;
    private Vector3 dragOffset;
    private Camera activeCamera;
    private Collider subCollider;
    private Transform grabbingControllerTransform = null;

    private void Awake()
    {
        if (bassAudioSource == null) bassAudioSource = GetComponent<AudioSource>();
        if (splMeter == null) splMeter = FindAnyObjectByType<VirtualSPLMeter>();
        subCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        UpdateActiveCamera();
        ClampToRoomBounds();
    }

    private void Update()
    {
        UpdateActiveCamera();
        HandleDesktopMouseInteraction();
        HandleVRControllerInteraction();

        // Enforce boundary constraints
        ClampToRoomBounds();

        // Notify SPL meter of subwoofer movement
        if (splMeter != null)
        {
            var env = FindAnyObjectByType<EnvironmentModifier>();
            bool treated = env != null && env.currentScenario == EnvironmentModifier.ScenarioType.Optimized;
            splMeter.UpdateMultiRowSPLBalance(transform.position, treated);
        }
    }

    private void LateUpdate()
    {
        ClampToRoomBounds();
    }

    private void UpdateActiveCamera()
    {
        if (activeCamera == null || !activeCamera.gameObject.activeInHierarchy)
        {
            activeCamera = Camera.main;
            if (activeCamera == null)
            {
                var cams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (cams.Length > 0) activeCamera = cams[0];
            }
        }
    }

    /// <summary>
    /// Clamps subwoofer transform to valid interior room coordinates and floor level.
    /// Prevents wall penetration or drifting into walls/screen.
    /// </summary>
    public void ClampToRoomBounds()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = floorY;
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }

    /// <summary>
    /// Snaps subwoofer to baseline corner placement.
    /// </summary>
    public void SetToBaselinePosition()
    {
        transform.position = baselineCornerPos;
        ClampToRoomBounds();
    }

    /// <summary>
    /// Snaps subwoofer to CEDIA-optimized placement.
    /// </summary>
    public void SetToOptimizedPosition()
    {
        transform.position = optimizedPlacementPos;
        ClampToRoomBounds();
    }

    // =========================================================================
    // DESKTOP INTERACTION (Mouse drag on floor plane)
    // =========================================================================

    private void HandleDesktopMouseInteraction()
    {
        if (activeCamera == null || IsVRGrabbed) return;

        Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
        Vector2 mousePos = Vector2.zero;
        bool pressedThisFrame = false;
        bool isHeld = false;
        bool releasedThisFrame = false;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            mousePos = mouse.position.ReadValue();
            pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
            isHeld = mouse.leftButton.isPressed;
            releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
        }
        else
#endif
        {
            try
            {
                mousePos = Input.mousePosition;
                pressedThisFrame = Input.GetMouseButtonDown(0);
                isHeld = Input.GetMouseButton(0);
                releasedThisFrame = Input.GetMouseButtonUp(0);
            }
            catch { return; }
        }

        if (pressedThisFrame)
        {
            Ray ray = activeCamera.ScreenPointToRay(mousePos);
            RaycastHit hit;
            if (subCollider != null && subCollider.Raycast(ray, out hit, 100f))
            {
                float enter;
                if (floorPlane.Raycast(ray, out enter))
                {
                    dragOffset = transform.position - ray.GetPoint(enter);
                    IsBeingDragged = true;
                }
            }
        }
        else if (isHeld && IsBeingDragged)
        {
            Ray ray = activeCamera.ScreenPointToRay(mousePos);
            float enter;
            if (floorPlane.Raycast(ray, out enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter) + dragOffset;
                transform.position = new Vector3(hitPoint.x, floorY, hitPoint.z);
                ClampToRoomBounds();
            }
        }
        else if (releasedThisFrame && IsBeingDragged)
        {
            IsBeingDragged = false;
            ClampToRoomBounds();
        }
    }

    // =========================================================================
    // VR INTERACTION (Meta Touch Controllers / OVRInput)
    // =========================================================================

    private void HandleVRControllerInteraction()
    {
        // Check if Right Touch or Left Touch is grabbing
        bool rightGrab = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
                         OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        bool leftGrab = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
                        OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

        if (!IsVRGrabbed)
        {
            if (rightGrab)
            {
                TryStartVRGrab(OVRInput.Controller.RTouch);
            }
            else if (leftGrab)
            {
                TryStartVRGrab(OVRInput.Controller.LTouch);
            }
        }
        else
        {
            // Currently VR grabbed
            bool stillHolding = (grabbingControllerTransform != null) &&
                (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger) || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger));

            if (stillHolding && grabbingControllerTransform != null)
            {
                Vector3 controllerPos = grabbingControllerTransform.position;
                Vector3 forward = grabbingControllerTransform.forward;
                Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
                Ray ray = new Ray(controllerPos, forward);
                float enter;

                if (floorPlane.Raycast(ray, out enter) && enter > 0f && enter < 8.0f)
                {
                    Vector3 targetPos = ray.GetPoint(enter);
                    transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
                }
                else
                {
                    // Fallback to controller XZ projection
                    transform.position = new Vector3(controllerPos.x, floorY, controllerPos.z);
                }
                ClampToRoomBounds();
            }
            else
            {
                IsVRGrabbed = false;
                grabbingControllerTransform = null;
                ClampToRoomBounds();
            }
        }
    }

    private void TryStartVRGrab(OVRInput.Controller controller)
    {
        // Find controller anchor transform
        string anchorName = (controller == OVRInput.Controller.RTouch) ? "RightControllerAnchor" : "LeftControllerAnchor";
        GameObject anchor = GameObject.Find(anchorName);
        if (anchor == null) return;

        Vector3 controllerPos = anchor.transform.position;
        Vector3 forward = anchor.transform.forward;

        // Proximity grab or ray pointing at subwoofer
        float distToSub = Vector3.Distance(controllerPos, transform.position);
        bool proximityHit = distToSub <= vrGrabDistance;

        Ray ray = new Ray(controllerPos, forward);
        RaycastHit hit;
        bool rayHit = subCollider != null && subCollider.Raycast(ray, out hit, 6.0f);

        if (proximityHit || rayHit)
        {
            IsVRGrabbed = true;
            grabbingControllerTransform = anchor.transform;
        }
    }
}
