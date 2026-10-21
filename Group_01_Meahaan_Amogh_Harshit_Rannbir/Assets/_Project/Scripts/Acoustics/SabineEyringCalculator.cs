// SabineEyringCalculator.cs - Analytical Reverberation Time (RT60) Calculation Engine
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Implements Sabine and Eyring-Norris classical statistical room acoustics formulations.

using System;
using System.Collections.Generic;
using UnityEngine;

public static class SabineEyringCalculator
{
    // Speed of sound in dry air at 20°C (m/s)
    public const float SoundSpeed = 343.2f;

    // Atmospheric attenuation coefficient at 1 kHz, 20°C, 50% RH (m^-1)
    public const float AirAttenuation1kHz = 0.0012f;

    /// <summary>
    /// Computes the reverberation time RT60 using the classical Sabine equation.
    /// Formulation: RT60 = (0.161 * V) / sum(S_i * alpha_i)
    /// Assumptions: Diffuse sound field, low and uniformly distributed absorption (alpha_mean < 0.20).
    /// </summary>
    /// <param name="volumeM3">Enclosure volume in cubic meters.</param>
    /// <param name="totalAbsorptionAreaMetricSabins">Equivalent absorption area A = sum(S_i * alpha_i).</param>
    /// <returns>Reverberation time RT60 in seconds, or float.PositiveInfinity if absorption is zero.</returns>
    public static float CalculateSabineRT60(float volumeM3, float totalAbsorptionAreaMetricSabins)
    {
        if (volumeM3 <= 0.0001f)
        {
            Debug.LogWarning("[SabineEyringCalculator] Invalid room volume: " + volumeM3);
            return 0f;
        }

        if (totalAbsorptionAreaMetricSabins <= 0.0001f)
        {
            return float.PositiveInfinity;
        }

        return (0.161f * volumeM3) / totalAbsorptionAreaMetricSabins;
    }

    /// <summary>
    /// Computes the reverberation time RT60 using the Eyring-Norris equation.
    /// Formulation: RT60 = (0.161 * V) / (-S * ln(1 - alpha_mean) + 4 * m * V)
    /// Assumptions: Diffuse sound field, valid for dead or heavily treated rooms (alpha_mean >= 0.20).
    /// </summary>
    /// <param name="volumeM3">Enclosure volume in cubic meters.</param>
    /// <param name="totalSurfaceAreaM2">Total interior surface area of all enclosing boundaries in m^2.</param>
    /// <param name="meanAbsorptionCoeff">Area-weighted mean absorption coefficient (alpha_mean in [0, 1]).</param>
    /// <param name="includeAirAbsorption">Whether to include atmospheric attenuation 4*m*V.</param>
    /// <returns>Reverberation time RT60 in seconds.</returns>
    public static float CalculateEyringRT60(float volumeM3, float totalSurfaceAreaM2, float meanAbsorptionCoeff, bool includeAirAbsorption = true)
    {
        if (volumeM3 <= 0.0001f || totalSurfaceAreaM2 <= 0.0001f)
        {
            Debug.LogWarning("[SabineEyringCalculator] Invalid geometry dimensions: V=" + volumeM3 + ", S=" + totalSurfaceAreaM2);
            return 0f;
        }

        if (meanAbsorptionCoeff <= 0.00001f)
        {
            return float.PositiveInfinity;
        }

        // Clamp mean absorption strictly below 1.0 to prevent ln(0) singularity
        float safeAlpha = Mathf.Clamp(meanAbsorptionCoeff, 0.00001f, 0.999f);

        float denominator = -totalSurfaceAreaM2 * Mathf.Log(1.0f - safeAlpha);

        if (includeAirAbsorption)
        {
            denominator += 4.0f * AirAttenuation1kHz * volumeM3;
        }

        if (denominator <= 0.00001f)
        {
            return float.PositiveInfinity;
        }

        return (0.161f * volumeM3) / denominator;
    }

    /// <summary>
    /// Calculates the CEDIA/CTA-RP22 target RT60 for a residential private listening room based on volume.
    /// Formulation: RT60_target = 0.30 * (V / 100)^(0.33) with standard residential range 0.20s to 0.55s.
    /// </summary>
    /// <param name="volumeM3">Room volume in m^3.</param>
    /// <param name="toleranceSeconds">Acceptable tolerance window around target (default +/- 0.15s).</param>
    /// <param name="lowerBound">Output lower compliance bound.</param>
    /// <param name="upperBound">Output upper compliance bound.</param>
    /// <returns>Nominal target RT60 in seconds.</returns>
    public static float CalculateCEDIATargetRT60(float volumeM3, float toleranceSeconds, out float lowerBound, out float upperBound)
    {
        float safeV = Mathf.Max(10.0f, volumeM3);
        float nominalTarget = 0.30f * Mathf.Pow(safeV / 100.0f, 0.333333f);

        // Standard CEDIA/CTA-RP22 Level 1-4 residential theater window is 0.20s to 0.55s (nominal ~0.35 - 0.45s)
        lowerBound = Mathf.Max(0.20f, nominalTarget - toleranceSeconds);
        upperBound = Mathf.Min(0.60f, nominalTarget + Mathf.Max(toleranceSeconds, 0.20f));

        return nominalTarget;
    }

    /// <summary>
    /// Checks if a measured/calculated RT60 is compliant with CEDIA/CTA-RP22.
    /// </summary>
    public static bool IsCEDIACompliant(float rt60, float volumeM3, out float targetRT60, out float lowerBound, out float upperBound)
    {
        targetRT60 = CalculateCEDIATargetRT60(volumeM3, 0.10f, out lowerBound, out upperBound);
        return rt60 >= lowerBound && rt60 <= upperBound;
    }
}
