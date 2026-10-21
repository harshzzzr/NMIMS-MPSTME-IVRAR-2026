========================================================================
IVRAR GROUP 01 - STREAMING ASSETS VIDEO & AUDIO DROP-IN FOLDER
========================================================================

You can play ANY video file directly on the cinema screen with synchronized
3D spatial audio simply by copying it into this folder:

OPTION A (Zero-Configuration Drop-In):
1. Name your video file: "movie.mp4"
2. Copy it into this folder: Assets/StreamingAssets/movie.mp4
3. Press Play in Unity! The cinema screen will automatically detect,
   load, and play the video with full spatial audio.

OPTION B (Custom Video File Name):
1. Copy any video file here (e.g., "my_trailer.mp4").
2. In the Unity Hierarchy, select:
   Cinema_Environment -> Screen_Stage -> Cinema_Screen
3. In the Inspector, under "Movie Screen Player":
   Change "Streaming Assets File Name" to "my_trailer.mp4".

SUPPORTED FORMATS:
- Video Codec: H.264 / AVC (MP4 container) or WebM (VP8/VP9)
- Audio Codec: AAC-LC Stereo or 5.1 Surround
- Recommended Resolution: 1080p (1920x1080, 16:9 aspect ratio)
- Frame Rate: 24 fps, 30 fps, or 60 fps
========================================================================
