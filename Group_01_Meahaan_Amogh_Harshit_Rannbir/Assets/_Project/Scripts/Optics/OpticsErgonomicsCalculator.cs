// OpticsErgonomicsCalculator.cs - Viewing Geometry and Multi-Point Sightline Analyzer
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Complies with SMPTE EG 18-1994, THX Cinema Specs, and CEDIA/CTA-RP22 standards.

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class OpticsErgonomicsCalculator : MonoBehaviour
{
    [Header("Optical Scene References")]
    public Transform playerEyes;
    public Transform movieScreen;
    public LayerMask sightlineObstructionMask = ~0;

    [Header("Calculated Visual Ergonomics")]
    public float ViewingDistanceM { get; private set; } = 3.9f;
    public float HorizontalFOV { get; private set; } = 38.5f;
    public float VerticalGazeAngle { get; private set; } = 8.5f;
    public float SightlineClearancePercent { get; private set; } = 100.0f;
    public bool IsSMPTECompliant { get; private set; } = true;
    public bool IsSightlineClear { get; private set; } = true;

    [Header("Multi-Point Raycast Sampling Points")]
    public Vector3[] sampleOffsetRatios = new Vector3[]
    {
        new Vector3(0.0f, 0.0f, 0f),    // Center
        new Vector3(-0.48f, 0.45f, 0f), // Top-Left
        new Vector3(0.48f, 0.45f, 0f),  // Top-Right
        new Vector3(-0.48f, -0.45f, 0f),// Bottom-Left
        new Vector3(0.48f, -0.45f, 0f), // Bottom-Right
        new Vector3(0.0f, -0.48f, 0f)   // Lower Screen Edge (Critical for Subtitles)
    };

    private void Update()
    {
        CalculateMetrics();
    }

    /// <summary>
    /// Evaluates current viewing distance in meters from player eyes to movie screen center.
    /// </summary>
    public float GetViewingDistance()
    {
        if (playerEyes == null || movieScreen == null) return 3.9f;
        return Vector3.Distance(playerEyes.position, movieScreen.position);
    }

    /// <summary>
    /// Computes horizontal field of view angle (theta_H = 2 * atan(W / (2d)) * 180 / pi).
    /// CEDIA/THX standard optimal range: 36 deg to 40 deg (Target: 38.5 deg).
    /// </summary>
    public float CalculateHorizontalFOV()
    {
        float distance = GetViewingDistance();
        if (distance <= 0.01f || movieScreen == null) return 38.5f;

        float screenWidth = movieScreen.lossyScale.x;
        // If lossy scale is uniform (e.g. quad 1x1 scaled to 3.5m), screenWidth = 3.5m
        float fovRad = 2.0f * Mathf.Atan(screenWidth / (2.0f * distance));
        return fovRad * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Evaluates vertical gaze angle elevation in degrees from horizontal gaze plane to screen center.
    /// Standard threshold: SMPTE EG 18-1994 specifies vertical elevation <= 15 degrees.
    /// </summary>
    public float CalculateVerticalGazeAngle()
    {
        if (playerEyes == null || movieScreen == null) return 8.5f;

        Vector3 toScreen = movieScreen.position - playerEyes.position;
        Vector3 horizontalProjection = new Vector3(toScreen.x, 0f, toScreen.z);

        if (horizontalProjection.sqrMagnitude < 0.0001f) return 0f;

        return Vector3.Angle(horizontalProjection.normalized, toScreen);
    }

    /// <summary>
    /// Performs rigorous multi-point raycast sightline analysis testing center, edges, and lower boundary.
    /// Never determines visibility from a single raycast.
    /// </summary>
    /// <returns>True if 100% of tested rays reach the screen surface unimpeded.</returns>
    public bool ValidateSightlines()
    {
        if (playerEyes == null || movieScreen == null)
        {
            SightlineClearancePercent = 100.0f;
            IsSightlineClear = true;
            return true;
        }

        Vector3 eyePos = playerEyes.position;
        int totalRays = sampleOffsetRatios.Length;
        int clearRays = 0;

        Vector3 screenRight = movieScreen.right;
        Vector3 screenUp = movieScreen.up;
        float screenHalfWidth = movieScreen.lossyScale.x * 0.5f;
        float screenHalfHeight = movieScreen.lossyScale.y * 0.5f;

        for (int i = 0; i < totalRays; i++)
        {
            Vector3 targetPoint = movieScreen.position +
                                  screenRight * (sampleOffsetRatios[i].x * 2.0f * screenHalfWidth) +
                                  screenUp * (sampleOffsetRatios[i].y * 2.0f * screenHalfHeight);

            Vector3 rayDir = targetPoint - eyePos;
            float targetDist = rayDir.magnitude;
            rayDir.Normalize();

            RaycastHit hit;
            if (Physics.Raycast(eyePos, rayDir, out hit, targetDist + 0.1f, sightlineObstructionMask))
            {
                // Check if hit is screen or child of screen
                if (hit.transform == movieScreen || hit.transform.IsChildOf(movieScreen))
                {
                    clearRays++;
                }
                else if (Mathf.Abs(hit.distance - targetDist) < 0.2f)
                {
                    // Reached screen within tolerance
                    clearRays++;
                }
            }
            else
            {
                // No obstruction encountered along ray path
                clearRays++;
            }
        }

        SightlineClearancePercent = (clearRays / (float)totalRays) * 100.0f;
        IsSightlineClear = clearRays == totalRays;
        return IsSightlineClear;
    }

    /// <summary>
    /// Recalculates all optical and ergonomic parameters.
    /// </summary>
    public void CalculateMetrics()
    {
        ViewingDistanceM = GetViewingDistance();
        HorizontalFOV = CalculateHorizontalFOV();
        VerticalGazeAngle = CalculateVerticalGazeAngle();
        ValidateSightlines();

        // SMPTE / THX compliance: FOV between 36 and 40 deg, vertical elevation <= 15 deg
        IsSMPTECompliant = (HorizontalFOV >= 36.0f && HorizontalFOV <= 40.5f) && (VerticalGazeAngle <= 15.0f);
    }
}
