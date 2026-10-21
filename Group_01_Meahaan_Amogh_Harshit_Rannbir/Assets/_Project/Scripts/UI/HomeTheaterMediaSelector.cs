// HomeTheaterMediaSelector.cs - Interactive World-Space & HUD Video Selection Console
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Provides a physical/interactive selection area in the home theater to choose which MP4 to play.
// Fully integrated with Meta XR Building Blocks ControllerButtonsMapper, VR ray, and Simulator mouse input.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class HomeTheaterMediaSelector : MonoBehaviour
{
    [Header("Target Movie Player")]
    public MovieScreenPlayer moviePlayer;

    [Header("Console Physical Placement")]
    public bool spawnWorldSpaceConsole = true;
    public Vector3 consolePosition = new Vector3(-1.15f, 0.85f, -0.3f);
    public Vector3 consoleRotation = new Vector3(15f, 40f, 0f);

    [Header("Simulator / Desktop On-Screen HUD Overlay")]
    public bool showDesktopOverlay = true;
    public KeyCode toggleKey = KeyCode.V;

    [Header("UI Styling")]
    public Color normalButtonColor = new Color(0.18f, 0.22f, 0.28f, 0.95f);
    public Color activePlayingColor = new Color(0.12f, 0.55f, 0.35f, 0.98f);
    public Color headerColor = new Color(0.1f, 0.6f, 0.9f, 1.0f);

    // Runtime state
    private List<string> videoList = new List<string>();
    private GUIStyle windowStyle;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeButtonStyle;
    private GUIStyle statusStyle;
    private Vector2 scrollPos;
    private bool stylesInitialized = false;

    private void Awake()
    {
        FindDependencies();
    }

    private void Start()
    {
        FindDependencies();
        RefreshPlaylist();
        if (spawnWorldSpaceConsole)
        {
            SetupWorldSpaceConsoleObject();
        }
    }

    public void ToggleMenu()
    {
        showDesktopOverlay = !showDesktopOverlay;
    }

    public void VolumeUp()
    {
        if (moviePlayer != null) moviePlayer.AdjustVolume(0.1f);
    }

    public void VolumeDown()
    {
        if (moviePlayer != null) moviePlayer.AdjustVolume(-0.1f);
    }

    private void Update()
    {
        HandleKeyboardShortcuts();
        HandleVRControllerInput();
    }

    private void HandleVRControllerInput()
    {
        if (moviePlayer == null) return;

        // --- RIGHT CONTROLLER ---
        // Button A (One) -> Next Video
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            moviePlayer.PlayNextVideo();
        }
        // Button B (Two) -> Play / Pause
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            moviePlayer.TogglePlayPause();
        }
        // Right Thumbstick Up -> Volume +
        if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickUp))
        {
            VolumeUp();
        }
        // Right Thumbstick Down -> Volume -
        if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstickDown))
        {
            VolumeDown();
        }

        // --- LEFT CONTROLLER ---
        // Button X (Three or One on LTouch) -> Previous Video
        if (OVRInput.GetDown(OVRInput.Button.Three) || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            moviePlayer.PlayPreviousVideo();
        }
        // Button Y (Four or Two on LTouch) -> Toggle Media Selector Menu
        if (OVRInput.GetDown(OVRInput.Button.Four) || OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            ToggleMenu();
        }
        // Menu Button (Start) -> Rescan Folder
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            RefreshPlaylist();
        }
    }

    public void FindDependencies()
    {
        if (moviePlayer == null)
        {
            moviePlayer = FindAnyObjectByType<MovieScreenPlayer>();
        }
    }

    public void RefreshPlaylist()
    {
        if (moviePlayer != null)
        {
            moviePlayer.RefreshAvailableVideos();
            videoList = moviePlayer.availableVideoFiles;
        }
        else
        {
            videoList.Clear();
            if (Directory.Exists(Application.streamingAssetsPath))
            {
                var files = Directory.GetFiles(Application.streamingAssetsPath, "*.*");
                foreach (var f in files)
                {
                    string ext = Path.GetExtension(f).ToLower();
                    if (ext == ".mp4" || ext == ".webm" || ext == ".mov")
                    {
                        videoList.Add(Path.GetFileName(f));
                    }
                }
            }
        }
    }

    private void HandleKeyboardShortcuts()
    {
        bool toggleMenu = false;
        bool nextVid = false;
        bool prevVid = false;
        bool togglePlay = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            toggleMenu = kb.vKey.wasPressedThisFrame;
            nextVid = kb.rightBracketKey.wasPressedThisFrame || kb.pageDownKey.wasPressedThisFrame;
            prevVid = kb.leftBracketKey.wasPressedThisFrame || kb.pageUpKey.wasPressedThisFrame;
            togglePlay = kb.spaceKey.wasPressedThisFrame;
        }
        else
#endif
        {
            try
            {
                if (Input.GetKeyDown(toggleKey)) toggleMenu = true;
                if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.PageDown)) nextVid = true;
                if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.PageUp)) prevVid = true;
            }
            catch { /* Ignore legacy input */ }
        }

        if (toggleMenu)
        {
            showDesktopOverlay = !showDesktopOverlay;
        }

        if (nextVid && moviePlayer != null)
        {
            moviePlayer.PlayNextVideo();
        }
        else if (prevVid && moviePlayer != null)
        {
            moviePlayer.PlayPreviousVideo();
        }
        else if (togglePlay && moviePlayer != null)
        {
            moviePlayer.TogglePlayPause();
        }
    }

    /// <summary>
    /// Creates or formats the physical 3D tablet stand in the theater room beside the viewer.
    /// </summary>
    private void SetupWorldSpaceConsoleObject()
    {
        transform.position = consolePosition;
        transform.rotation = Quaternion.Euler(consoleRotation);

        // Check if pedestal child already exists
        Transform existingPedestal = transform.Find("Kiosk_Pedestal");
        if (existingPedestal == null)
        {
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Kiosk_Pedestal";
            pedestal.transform.SetParent(transform, false);
            pedestal.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            pedestal.transform.localScale = new Vector3(0.08f, 0.42f, 0.08f);

            // Dark matte material
            var renderer = pedestal.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                Material mat = new Material(unlit) { color = new Color(0.12f, 0.12f, 0.14f) };
                renderer.material = mat;
            }

            // Tablet backplate
            GameObject tablet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tablet.name = "Kiosk_Tablet";
            tablet.transform.SetParent(transform, false);
            tablet.transform.localPosition = Vector3.zero;
            tablet.transform.localScale = new Vector3(0.48f, 0.32f, 0.02f);

            var tabRenderer = tablet.GetComponent<MeshRenderer>();
            if (tabRenderer != null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                Material mat = new Material(unlit) { color = new Color(0.06f, 0.07f, 0.09f) };
                tabRenderer.material = mat;
            }
        }
    }

    /// <summary>
    /// Interactive OnGUI overlay for Simulator and Desktop testing.
    /// Lets users immediately click and choose videos with the mouse or VR controller button mapping.
    /// </summary>
    private void OnGUI()
    {
        if (!showDesktopOverlay) return;

        InitStyles();

        float width = 360f;
        float height = 380f;
        float x = 20f;
        float y = Screen.height - height - 25f;

        GUILayout.BeginArea(new Rect(x, y, width, height), windowStyle);

        // Header
        GUILayout.BeginHorizontal();
        GUILayout.Label("🎬 THEATER MEDIA SELECTOR", headerStyle);
        if (GUILayout.Button("✕", GUILayout.Width(28), GUILayout.Height(24)))
        {
            showDesktopOverlay = false;
        }
        GUILayout.EndHorizontal();

        // Live Status
        string activeVid = (moviePlayer != null) ? moviePlayer.streamingAssetsFileName : "None";
        string playStatus = (moviePlayer != null) ? moviePlayer.PlaybackStatus : "Idle";
        GUILayout.Label($"Playing: <b>{activeVid}</b> [{playStatus}]", statusStyle);

        GUILayout.Space(6);
        GUILayout.Label("Available Videos (Assets/StreamingAssets/):", statusStyle);

        // Video Playlist Scroll Area
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(175));
        if (videoList == null || videoList.Count == 0)
        {
            RefreshPlaylist();
        }

        if (videoList != null && videoList.Count > 0)
        {
            for (int i = 0; i < videoList.Count; i++)
            {
                string vidName = videoList[i];
                bool isCurrent = (moviePlayer != null && moviePlayer.streamingAssetsFileName == vidName);

                GUIStyle styleToUse = isCurrent ? activeButtonStyle : buttonStyle;
                string label = isCurrent ? $"▶  {vidName}  [ACTIVE]" : $"   {vidName}";

                if (GUILayout.Button(label, styleToUse, GUILayout.Height(30)))
                {
                    if (moviePlayer != null)
                    {
                        moviePlayer.PlayStreamingAsset(vidName);
                    }
                }
            }
        }
        else
        {
            GUILayout.Label("No video files found in StreamingAssets.", statusStyle);
            if (GUILayout.Button("↻ Rescan Folder", buttonStyle, GUILayout.Height(28)))
            {
                RefreshPlaylist();
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(8);

        // Control Toolbar
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⏮ Prev", buttonStyle, GUILayout.Height(32)))
        {
            if (moviePlayer != null) moviePlayer.PlayPreviousVideo();
        }

        string toggleText = (moviePlayer != null && moviePlayer.IsPlaying && !moviePlayer.IsPaused) ? "⏸ Pause" : "▶ Play";
        if (GUILayout.Button(toggleText, activeButtonStyle, GUILayout.Height(32)))
        {
            if (moviePlayer != null) moviePlayer.TogglePlayPause();
        }

        if (GUILayout.Button("⏭ Next", buttonStyle, GUILayout.Height(32)))
        {
            if (moviePlayer != null) moviePlayer.PlayNextVideo();
        }

        if (GUILayout.Button("🔄 Loop", buttonStyle, GUILayout.Height(32)))
        {
            if (moviePlayer != null) moviePlayer.RestartMovie();
        }
        GUILayout.EndHorizontal();

        // Volume controls
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Vol: {(moviePlayer != null ? Mathf.RoundToInt(moviePlayer.playbackVolume * 100) : 85)}%", statusStyle, GUILayout.Width(65));
        if (GUILayout.Button("🔉 -", buttonStyle, GUILayout.Width(45), GUILayout.Height(22)))
        {
            if (moviePlayer != null) moviePlayer.AdjustVolume(-0.1f);
        }
        if (GUILayout.Button("🔊 +", buttonStyle, GUILayout.Width(45), GUILayout.Height(22)))
        {
            if (moviePlayer != null) moviePlayer.AdjustVolume(0.1f);
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label("<color=#888888>Press [V] to Hide | Controller [A]/[B]</color>", statusStyle);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void InitStyles()
    {
        if (stylesInitialized && windowStyle != null) return;

        windowStyle = new GUIStyle(GUI.skin.box);
        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0.07f, 0.08f, 0.11f, 0.94f));
        bgTex.Apply();
        windowStyle.normal.background = bgTex;
        windowStyle.padding = new RectOffset(12, 12, 10, 10);

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        headerStyle.normal.textColor = new Color(0.2f, 0.75f, 1.0f);

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            richText = true,
            alignment = TextAnchor.MiddleLeft
        };
        statusStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft
        };
        buttonStyle.normal.textColor = Color.white;

        activeButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        activeButtonStyle.normal.textColor = new Color(0.4f, 1.0f, 0.6f);

        stylesInitialized = true;
    }
}
