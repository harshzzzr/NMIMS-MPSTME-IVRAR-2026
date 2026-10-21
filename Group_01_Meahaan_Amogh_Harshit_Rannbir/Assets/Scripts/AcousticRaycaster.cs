// AcousticRaycaster.cs - Real-Time Geometrical Acoustic Raycasting Engine
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Course: Introduction to Virtual Reality and Augmented Reality (702COI002)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AcousticRaycaster : MonoBehaviour
{
    [Header("Raycast Engine Configuration")]
    [Tooltip("Total number of rays emitted per loudspeaker source")]
    [Range(100, 5000)]
    public int raysPerSource = 1000;

    [Tooltip("Maximum specular reflection bounces per ray")]
    [Range(1, 12)]
    public int maxBounces = 6;

    [Tooltip("Speed of sound in dry air at 20°C (m/s)")]
    public float soundSpeed = 343.0f;

    [Tooltip("Spherical listener reception radius in meters")]
    public float receiverRadius = 0.50f;

    [Header("Octave Bands (Hz)")]
    public float[] octaveBands = new float[] { 125f, 250f, 500f, 1000f, 2000f, 4000f };

    [Header("Scene References")]
    public Transform primaryListeningPosition;
    public Transform[] loudspeakerSources;
    public LayerMask acousticSurfacesLayer = ~0;

    [Header("Impulse Response Accumulator")]
    public float timeResolutionMs = 5.0f;
    public int maxTimeBins = 400; // 2.0 seconds at 5ms/bin
    private float[] impulseResponseHistogram;

    // Simulation outputs
    public float LastComputedRT60 { get; private set; } = 1.40f;
    public float LastComputedT20 { get; private set; } = 0.467f;
    public float LastEnergyDecayedPercent { get; private set; } = 100f;
    public int TotalRaysCast { get; private set; } = 0;
    public int TotalRaysReceived { get; private set; } = 0;

    // References to coupled student subsystems
    public RT60TelemetryLogger telemetryLogger;

    private void Awake()
    {
        impulseResponseHistogram = new float[maxTimeBins];
        if (telemetryLogger == null)
        {
            telemetryLogger = FindAnyObjectByType<RT60TelemetryLogger>();
        }
    }

    void Start()
    {
        Debug.Log("[IVRAR Group 01] Initialized Acoustic Raycaster. Ready for acoustic simulations.");
    }

    // =========================================================================
    // STUDENT 1: Meahaan Sharma (I066) - Lead XR Systems Architect
    // Branch: feat/i066-xr-systems-architect
    // =========================================================================

    /// <summary>
    /// Generates uniform ray direction unit vectors across 4pi steradians using the spherical Fibonacci lattice.
    /// Eliminates polar clustering inherent in naive spherical coordinate random generation.
    /// </summary>
    /// <param name="count">Total number of rays to emit per loudspeaker source.</param>
    /// <returns>Array of normalized Vector3 direction vectors.</returns>
    public Vector3[] GenerateUniformRayDirections(int count)
    {
        if (count <= 0)
        {
            return new Vector3[1] { Vector3.forward };
        }

        Vector3[] dirs = new Vector3[count];
        float phi = Mathf.PI * (3.0f - Mathf.Sqrt(5.0f)); // Golden angle approx 2.399963 rad

        for (int i = 0; i < count; i++)
        {
            float y = 1.0f - (i / (float)Mathf.Max(1, count - 1)) * 2.0f;
            float radius = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - y * y));
            float theta = phi * i;

            float x = Mathf.Cos(theta) * radius;
            float z = Mathf.Sin(theta) * radius;

            dirs[i] = new Vector3(x, y, z).normalized;
        }
        return dirs;
    }

    /// <summary>
    /// Evaluates vertical and horizontal visual sightline clearance from viewer position to the screen center.
    /// SMPTE EG 18-1994 / THX standard requires vertical elevation <= 15 degrees.
    /// </summary>
    public bool EvaluateSightlineClearance(Vector3 viewerEye, Vector3 screenCenter, out float gazeAngle)
    {
        Vector3 toScreen = screenCenter - viewerEye;
        Vector3 horizontalProjection = new Vector3(toScreen.x, 0f, toScreen.z).normalized;

        if (horizontalProjection.sqrMagnitude < 0.0001f)
        {
            gazeAngle = 0f;
            return true;
        }

        gazeAngle = Vector3.Angle(horizontalProjection, toScreen);
        return gazeAngle <= 15.0f;
    }

    // =========================================================================
    // STUDENT 2: Amogh Gupta (C034) - Spatial Acoustics & Audio Specialist
    // Branch: feat/c034-spatial-acoustics-au
    // =========================================================================

    /// <summary>
    /// Computes specular reflection direction vector via Snell's Law and applies frequency-dependent absorption.
    /// r = d - 2.0 * dot(d, n) * n
    /// </summary>
    public Vector3 ComputeSpecularReflection(Vector3 incidentDir, Vector3 surfaceNormal, float[] energyBands, float[] materialAlphas)
    {
        Vector3 refl = incidentDir - 2.0f * Vector3.Dot(incidentDir, surfaceNormal) * surfaceNormal;

        if (energyBands != null && materialAlphas != null)
        {
            int bands = Mathf.Min(energyBands.Length, materialAlphas.Length);
            for (int k = 0; k < bands; k++)
            {
                float alpha = Mathf.Clamp01(materialAlphas[k]);
                energyBands[k] *= Mathf.Clamp01(1.0f - alpha);
            }
        }
        return refl.normalized;
    }

    // =========================================================================
    // STUDENT 3: Harshit Rai (N083) - Human Factors & Usability Engineer
    // Branch: feat/n083-human-factors-usabil
    // =========================================================================

    /// <summary>
    /// Tests whether an acoustic ray intersects the primary listening position receiver sphere and computes arrival delay.
    /// </summary>
    public bool DetectReceiverIntersection(Vector3 rayOrigin, Vector3 rayDir, float rayLength, out float arrivalDelay)
    {
        arrivalDelay = 0.0f;
        if (primaryListeningPosition == null) return false;

        Vector3 toReceiver = primaryListeningPosition.position - rayOrigin;
        float projection = Vector3.Dot(toReceiver, rayDir);

        if (projection > 0f && projection <= rayLength)
        {
            float perpDistSq = toReceiver.sqrMagnitude - (projection * projection);
            if (perpDistSq <= (receiverRadius * receiverRadius))
            {
                arrivalDelay = projection / soundSpeed;
                return true;
            }
        }
        return false;
    }

    // =========================================================================
    // FULL MONTE CARLO SIMULATION EXECUTION
    // =========================================================================

    /// <summary>
    /// Executes a full real-time acoustic raycast simulation across all active loudspeaker sources.
    /// </summary>
    public float RunSimulation()
    {
        if (loudspeakerSources == null || loudspeakerSources.Length == 0)
        {
            Debug.LogWarning("[AcousticRaycaster] No loudspeaker sources assigned.");
            return LastComputedRT60;
        }

        if (primaryListeningPosition == null)
        {
            Debug.LogWarning("[AcousticRaycaster] Primary listening position is not set.");
            return LastComputedRT60;
        }

        // Reset histogram
        if (impulseResponseHistogram == null || impulseResponseHistogram.Length != maxTimeBins)
        {
            impulseResponseHistogram = new float[maxTimeBins];
        }
        Array.Clear(impulseResponseHistogram, 0, impulseResponseHistogram.Length);
        TotalRaysCast = 0;
        TotalRaysReceived = 0;

        Vector3[] rayDirections = GenerateUniformRayDirections(raysPerSource);
        float binWidthSeconds = timeResolutionMs / 1000.0f;

        float[] defaultAlphas = new float[6] { 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f };

        foreach (var source in loudspeakerSources)
        {
            if (source == null || !source.gameObject.activeInHierarchy) continue;

            Vector3 sourcePos = source.position;

            for (int r = 0; r < rayDirections.Length; r++)
            {
                TotalRaysCast++;
                Vector3 currentOrigin = sourcePos;
                Vector3 currentDir = rayDirections[r];
                float totalPathLength = 0f;

                float[] rayEnergyBands = new float[6] { 1f, 1f, 1f, 1f, 1f, 1f };

                for (int bounce = 0; bounce < maxBounces; bounce++)
                {
                    RaycastHit hit;
                    float maxRayDist = 50f;

                    if (Physics.Raycast(currentOrigin, currentDir, out hit, maxRayDist, acousticSurfacesLayer))
                    {
                        float segmentDist = hit.distance;

                        // Check direct receiver intersection along this ray segment
                        float delay;
                        if (DetectReceiverIntersection(currentOrigin, currentDir, segmentDist, out delay))
                        {
                            float arrivalTime = (totalPathLength + delay * soundSpeed) / soundSpeed;
                            int binIndex = Mathf.FloorToInt(arrivalTime / binWidthSeconds);

                            if (binIndex >= 0 && binIndex < maxTimeBins)
                            {
                                // Mid-band energy weight (1000 Hz band)
                                float energy = rayEnergyBands[3] / Mathf.Max(1.0f, arrivalTime * arrivalTime);
                                impulseResponseHistogram[binIndex] += energy;
                                TotalRaysReceived++;
                            }
                        }

                        totalPathLength += segmentDist;

                        // Determine surface absorption properties
                        float[] surfaceAlphas = defaultAlphas;
                        var propComponent = hit.collider.GetComponent<AcousticSurfaceProperties>();
                        if (propComponent != null && propComponent.materialData != null)
                        {
                            surfaceAlphas = propComponent.materialData.octaveBandAbsorption;
                        }

                        // Compute reflection direction & attenuate energy
                        currentDir = ComputeSpecularReflection(currentDir, hit.normal, rayEnergyBands, surfaceAlphas);
                        currentOrigin = hit.point + hit.normal * 0.001f; // offset from surface
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // Compute Schroeder RT60 from accumulated impulse response
        if (telemetryLogger != null)
        {
            LastComputedRT60 = telemetryLogger.ComputeSchroederRT60(impulseResponseHistogram);
        }
        else
        {
            LastComputedRT60 = ComputeSchroederRT60Direct(impulseResponseHistogram);
        }

        Debug.Log($"[AcousticRaycaster] Raycast complete. Emitted: {TotalRaysCast}, Hits: {TotalRaysReceived}, Schroeder RT60: {LastComputedRT60:F3}s");
        return LastComputedRT60;
    }

    /// <summary>
    /// Direct calculation of Schroeder RT60 if telemetry logger is not bound.
    /// </summary>
    public float ComputeSchroederRT60Direct(float[] impulseResponse)
    {
        int n = impulseResponse != null ? impulseResponse.Length : 0;
        if (n == 0) return 1.40f;

        float[] energyDecay = new float[n];
        float sum = 0.0f;

        for (int i = n - 1; i >= 0; i--)
        {
            sum += impulseResponse[i] * impulseResponse[i];
            energyDecay[i] = sum;
        }

        float totalEnergy = energyDecay[0];
        if (totalEnergy <= 0.00001f) return 1.40f;

        float[] decayDb = new float[n];
        for (int i = 0; i < n; i++)
        {
            decayDb[i] = 10.0f * Mathf.Log10(Mathf.Max(0.00001f, energyDecay[i] / totalEnergy));
        }

        float t5 = -1f, t25 = -1f;
        float binDt = timeResolutionMs / 1000.0f;
        for (int i = 0; i < n; i++)
        {
            if (t5 < 0 && decayDb[i] <= -5.0f) t5 = i * binDt;
            if (t25 < 0 && decayDb[i] <= -25.0f) t25 = i * binDt;
        }

        if (t5 >= 0 && t25 > t5)
        {
            float t20 = t25 - t5;
            LastComputedT20 = t20;
            return t20 * 3.0f;
        }
        return 1.40f;
    }
}
