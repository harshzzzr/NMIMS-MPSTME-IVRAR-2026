// MovieScreenPlayer.cs - Universal Video & Synchronized Audio Playback Controller
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Works seamlessly on Windows Desktop, Windows PCVR, and Meta Quest 2 (Android).

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class MovieScreenPlayer : MonoBehaviour
{
    [Header("Screen & Renderer Binding")]
    public MeshRenderer cinemaScreenRenderer;
    public RenderTexture movieRenderTexture;

    [Header("Video & Audio Components")]
    public VideoPlayer videoPlayer;
    public AudioSource screenAudioSource;

    [Header("Video Source (Clip, URL, or StreamingAssets)")]
    [Tooltip("Drag and drop any imported .mp4, .mov, or .webm video asset here")]
    public VideoClip movieClip;

    [Tooltip("Optional direct web URL (e.g. https://.../trailer.mp4) or file:// URL")]
    public string videoURL = "";

    [Tooltip("Filename inside Assets/StreamingAssets/ to play automatically (e.g. movie.mp4)")]
    public string streamingAssetsFileName = "movie.mp4";

    [Header("StreamingAssets Dynamic Playlist")]
    public System.Collections.Generic.List<string> availableVideoFiles = new System.Collections.Generic.List<string>();
    public int currentVideoIndex = 0;

    [Header("Playback Settings")]
    [Range(0f, 1f)] public float playbackVolume = 0.85f;
    public bool playOnStart = true;
    public bool loopMovie = true;

    [Header("Fallback Procedural AV Generator")]
    public bool useProceduralFallbackIfNoClip = true;
    private Texture2D proceduralTestTexture;
    private Color32[] testColors;
    private bool isUsingProcedural = false;
    private float proceduralTimer = 0f;

    // Playback state
    public bool IsPlaying { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    public string PlaybackStatus { get; private set; } = "Stopped";

    private Material screenMaterialInstance;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        SetupScreenMaterial();

        if (playOnStart)
        {
            PlayMovie();
        }
    }

    private void Update()
    {
        if (isUsingProcedural && IsPlaying && !IsPaused)
        {
            UpdateProceduralMoviePattern();
        }
    }

    /// <summary>
    /// Ensures VideoPlayer, AudioSource, and RenderTexture are validly created and wired.
    /// </summary>
    public void InitializeComponents()
    {
        if (cinemaScreenRenderer == null)
        {
            cinemaScreenRenderer = GetComponent<MeshRenderer>();
        }

        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        if (screenAudioSource == null)
        {
            screenAudioSource = GetComponent<AudioSource>();
            if (screenAudioSource == null) screenAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (movieRenderTexture == null)
        {
            movieRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            movieRenderTexture.name = "RT_CinemaScreen";
            movieRenderTexture.Create();
        }

        // Configure VideoPlayer
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loopMovie;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = movieRenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        // Configure AudioSource
        screenAudioSource.playOnAwake = false;
        screenAudioSource.loop = loopMovie;
        screenAudioSource.spatialBlend = 1.0f; // 3D Spatial Audio located at center of screen
        screenAudioSource.minDistance = 1.0f;
        screenAudioSource.maxDistance = 25.0f;
        screenAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        screenAudioSource.volume = playbackVolume;

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;
    }

    /// <summary>
    /// Configures the virtual cinema screen material using a URP Unlit shader for maximum clarity and color vibrancy.
    /// Disables backface culling and ensures the Quad faces forward into the room so the screen is always visible.
    /// </summary>
    public void SetupScreenMaterial()
    {
        if (cinemaScreenRenderer == null) return;

        // Ensure the screen Quad faces into the room (towards -Z where the viewer sits)
        if (cinemaScreenRenderer.transform.localEulerAngles.y > 90f && cinemaScreenRenderer.transform.localEulerAngles.y < 270f)
        {
            cinemaScreenRenderer.transform.localRotation = Quaternion.identity;
        }

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");
        if (unlitShader == null) unlitShader = Shader.Find("Standard");

        screenMaterialInstance = new Material(unlitShader);
        screenMaterialInstance.name = "Mat_CinemaScreen_Active";

        // Double-sided rendering: disable backface culling (_Cull = 0) so the screen is never invisible
        if (screenMaterialInstance.HasProperty("_Cull"))
        {
            screenMaterialInstance.SetFloat("_Cull", 0f);
        }

        if (movieRenderTexture != null)
        {
            if (screenMaterialInstance.HasProperty("_BaseMap"))
                screenMaterialInstance.SetTexture("_BaseMap", movieRenderTexture);
            if (screenMaterialInstance.HasProperty("_MainTex"))
                screenMaterialInstance.SetTexture("_MainTex", movieRenderTexture);
        }

        if (screenMaterialInstance.HasProperty("_BaseColor"))
            screenMaterialInstance.SetColor("_BaseColor", Color.white);
        if (screenMaterialInstance.HasProperty("_Color"))
            screenMaterialInstance.SetColor("_Color", Color.white);

        cinemaScreenRenderer.material = screenMaterialInstance;
    }

    [ContextMenu("Play Movie")]
    public void PlayMovie()
    {
        InitializeComponents();
        SetupScreenMaterial();

        // 1. Check assigned VideoClip
        if (movieClip != null)
        {
            isUsingProcedural = false;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = movieClip;
            PlaybackStatus = $"Preparing '{movieClip.name}'";
            Debug.Log($"[MovieScreenPlayer] Preparing VideoClip: {movieClip.name}");
            videoPlayer.Prepare();
            return;
        }

        // 2. Check direct URL
        if (!string.IsNullOrEmpty(videoURL))
        {
            isUsingProcedural = false;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoURL;
            PlaybackStatus = "Preparing URL Stream";
            Debug.Log($"[MovieScreenPlayer] Preparing URL: {videoURL}");
            videoPlayer.Prepare();
            return;
        }

        // 3. Check StreamingAssets file
        if (!string.IsNullOrEmpty(streamingAssetsFileName))
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, streamingAssetsFileName);
            if (File.Exists(streamingPath))
            {
                isUsingProcedural = false;
                videoPlayer.source = VideoSource.Url;
                // Normalize file path with forward slashes for Unity's Media Foundation URL decoder
                videoPlayer.url = streamingPath.Replace('\\', '/');
                PlaybackStatus = $"Preparing StreamingAssets '{streamingAssetsFileName}'";
                Debug.Log($"[MovieScreenPlayer] Found video in StreamingAssets: {videoPlayer.url}");
                videoPlayer.Prepare();
                return;
            }
        }

        // 4. Check if VideoPlayer itself already has a clip or URL
        if (videoPlayer.clip != null || !string.IsNullOrEmpty(videoPlayer.url))
        {
            isUsingProcedural = false;
            PlaybackStatus = "Preparing VideoPlayer Content";
            videoPlayer.Prepare();
            return;
        }

        // 5. Procedural Fallback if no video file supplied
        if (useProceduralFallbackIfNoClip)
        {
            StartProceduralPlayback();
        }
    }

    [ContextMenu("Pause Movie")]
    public void PauseMovie()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        if (screenAudioSource != null && screenAudioSource.isPlaying)
        {
            screenAudioSource.Pause();
        }
        IsPaused = true;
        PlaybackStatus = "Paused";
    }

    [ContextMenu("Resume Movie")]
    public void ResumeMovie()
    {
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            videoPlayer.Play();
        }
        if (screenAudioSource != null)
        {
            screenAudioSource.UnPause();
        }
        IsPaused = false;
        PlaybackStatus = "Playing";
    }

    [ContextMenu("Restart Movie")]
    public void RestartMovie()
    {
        if (videoPlayer != null)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
        if (screenAudioSource != null)
        {
            screenAudioSource.time = 0;
            screenAudioSource.Play();
        }
        IsPaused = false;
        PlaybackStatus = "Playing";
    }

    public void SetVolume(float volume)
    {
        playbackVolume = Mathf.Clamp01(volume);
        if (screenAudioSource != null)
        {
            screenAudioSource.volume = playbackVolume;
        }
        if (videoPlayer != null)
        {
            videoPlayer.SetDirectAudioVolume(0, playbackVolume);
        }
    }

    public void AdjustVolume(float delta)
    {
        SetVolume(playbackVolume + delta);
    }

    /// <summary>
    /// Scans Assets/StreamingAssets for all valid .mp4, .webm, and .mov files.
    /// </summary>
    public void RefreshAvailableVideos()
    {
        availableVideoFiles.Clear();
        if (Directory.Exists(Application.streamingAssetsPath))
        {
            var files = Directory.GetFiles(Application.streamingAssetsPath, "*.*");
            foreach (var f in files)
            {
                string ext = Path.GetExtension(f).ToLower();
                if (ext == ".mp4" || ext == ".webm" || ext == ".mov")
                {
                    string filename = Path.GetFileName(f);
                    if (!availableVideoFiles.Contains(filename))
                    {
                        availableVideoFiles.Add(filename);
                    }
                }
            }
        }

        if (availableVideoFiles.Count == 0 && !string.IsNullOrEmpty(streamingAssetsFileName))
        {
            availableVideoFiles.Add(streamingAssetsFileName);
        }

        currentVideoIndex = Mathf.Clamp(currentVideoIndex, 0, Mathf.Max(0, availableVideoFiles.Count - 1));
    }

    /// <summary>
    /// Switches playback immediately to the specified video inside Assets/StreamingAssets.
    /// </summary>
    public void PlayStreamingAsset(string fileName)
    {
        streamingAssetsFileName = fileName;
        movieClip = null;
        videoURL = "";
        RefreshAvailableVideos();
        if (availableVideoFiles.Contains(fileName))
        {
            currentVideoIndex = availableVideoFiles.IndexOf(fileName);
        }
        PlayMovie();
    }

    /// <summary>
    /// Switches playback to the video at the specified playlist index.
    /// </summary>
    public void PlayVideoIndex(int index)
    {
        RefreshAvailableVideos();
        if (availableVideoFiles.Count == 0) return;
        currentVideoIndex = Mathf.Clamp(index, 0, availableVideoFiles.Count - 1);
        PlayStreamingAsset(availableVideoFiles[currentVideoIndex]);
    }

    /// <summary>
    /// Plays the next video in the playlist (loops around).
    /// </summary>
    public void PlayNextVideo()
    {
        RefreshAvailableVideos();
        if (availableVideoFiles.Count == 0) return;
        currentVideoIndex = (currentVideoIndex + 1) % availableVideoFiles.Count;
        PlayStreamingAsset(availableVideoFiles[currentVideoIndex]);
    }

    /// <summary>
    /// Plays the previous video in the playlist (loops around).
    /// </summary>
    public void PlayPreviousVideo()
    {
        RefreshAvailableVideos();
        if (availableVideoFiles.Count == 0) return;
        currentVideoIndex = (currentVideoIndex - 1 + availableVideoFiles.Count) % availableVideoFiles.Count;
        PlayStreamingAsset(availableVideoFiles[currentVideoIndex]);
    }

    /// <summary>
    /// Toggles between Play and Pause.
    /// </summary>
    public void TogglePlayPause()
    {
        if (videoPlayer == null) return;
        if (IsPlaying && !IsPaused)
        {
            PauseMovie();
        }
        else if (IsPaused)
        {
            ResumeMovie();
        }
        else
        {
            PlayMovie();
        }
    }

    [ContextMenu("Load Sample Online Movie Trailer")]
    public void LoadSampleTrailer()
    {
        videoURL = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
        PlayMovie();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        // Dynamically map audio track if present
        if (source.audioTrackCount > 0)
        {
            source.controlledAudioTrackCount = 1;
            source.EnableAudioTrack(0, true);
            source.SetTargetAudioSource(0, screenAudioSource);
            if (screenAudioSource != null && !screenAudioSource.isPlaying)
            {
                screenAudioSource.Play();
            }
            Debug.Log($"[MovieScreenPlayer] Video audio track 0 mapped to spatial audio source successfully.");
        }
        else
        {
            Debug.LogWarning("[MovieScreenPlayer] Video prepared successfully, but NO audio tracks were found in this file (audioTrackCount is 0).");
        }

        source.Play();
        IsPlaying = true;
        IsPaused = false;
        PlaybackStatus = $"Playing ({source.clip?.name ?? Path.GetFileName(source.url)})";
        Debug.Log("[MovieScreenPlayer] Video playback started successfully.");
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[MovieScreenPlayer] Video playback failed: {message}\n" +
            "Common causes:\n" +
            "1. Fragmented MP4 (fMP4) or missing faststart moov atom (re-export without fragmented headers).\n" +
            "2. Unsupported codec (file must be H.264/AVC video + AAC audio).\n" +
            "3. Audio track missing or unreadable.\n" +
            "Falling back to dynamic procedural SMPTE test pattern with 440Hz/1kHz calibration tone.");
        StartProceduralPlayback();
    }

    private void StartProceduralPlayback()
    {
        isUsingProcedural = true;
        IsPlaying = true;
        IsPaused = false;
        PlaybackStatus = "Playing (Interactive SMPTE Test Pattern)";

        int width = 256;
        int height = 144;
        if (proceduralTestTexture == null)
        {
            proceduralTestTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            proceduralTestTexture.filterMode = FilterMode.Bilinear;
            testColors = new Color32[width * height];
        }

        // Play synthetic test tone / dialogue simulation on audio source
        if (screenAudioSource != null && !screenAudioSource.isPlaying)
        {
            screenAudioSource.clip = CreateSyntheticTestAudioClip();
            screenAudioSource.loop = true;
            screenAudioSource.Play();
        }
    }

    private void UpdateProceduralMoviePattern()
    {
        proceduralTimer += Time.deltaTime;
        int width = proceduralTestTexture.width;
        int height = proceduralTestTexture.height;

        float t = proceduralTimer * 2.0f;
        byte rPulse = (byte)(Mathf.PingPong(t * 40f, 180f) + 40);
        byte gPulse = (byte)(Mathf.PingPong(t * 25f, 150f) + 50);
        byte bPulse = (byte)(Mathf.PingPong(t * 30f, 200f) + 55);

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            float ny = (float)y / height;

            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                if (ny > 0.45f)
                {
                    int bar = Mathf.FloorToInt(nx * 7.0f);
                    Color32 barColor = bar switch
                    {
                        0 => new Color32(200, 200, 200, 255), // White
                        1 => new Color32(200, 200, 0, 255),   // Yellow
                        2 => new Color32(0, 200, 200, 255),   // Cyan
                        3 => new Color32(0, 200, 0, 255),     // Green
                        4 => new Color32(200, 0, 200, 255),   // Magenta
                        5 => new Color32(200, 0, 0, 255),     // Red
                        _ => new Color32(0, 0, 200, 255)      // Blue
                    };
                    testColors[row + x] = barColor;
                }
                else
                {
                    float wave = Mathf.Sin(nx * 12.0f + t) * 0.5f + 0.5f;
                    testColors[row + x] = new Color32(
                        (byte)(rPulse * wave),
                        (byte)(gPulse * (1f - wave)),
                        bPulse,
                        255
                    );
                }
            }
        }

        proceduralTestTexture.SetPixels32(testColors);
        proceduralTestTexture.Apply();

        Graphics.Blit(proceduralTestTexture, movieRenderTexture);
    }

    /// <summary>
    /// Generates a smooth, calibrated 440 Hz / 1 kHz alternating cinema audio test tone.
    /// </summary>
    private AudioClip CreateSyntheticTestAudioClip()
    {
        int sampleRate = 44100;
        int lengthSamples = sampleRate * 4; // 4 seconds loop
        float[] samples = new float[lengthSamples];

        for (int i = 0; i < lengthSamples; i++)
        {
            float time = i / (float)sampleRate;
            float freq = (time % 2.0f < 1.0f) ? 440.0f : 1000.0f; // 440Hz / 1kHz reference
            float sine = Mathf.Sin(2.0f * Mathf.PI * freq * time);
            samples[i] = sine * 0.15f;
        }

        AudioClip clip = AudioClip.Create("SyntheticCinemaAudio", lengthSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
