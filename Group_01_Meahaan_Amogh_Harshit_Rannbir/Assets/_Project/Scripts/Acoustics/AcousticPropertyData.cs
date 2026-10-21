// AcousticPropertyData.cs - ScriptableObject defining physical and acoustic material properties
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Universal Render Pipeline · Cross-Platform Desktop/VR/Quest

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAcousticMaterial", menuName = "AV Simulator/Acoustic Material Data", order = 1)]
public class AcousticPropertyData : ScriptableObject
{
    [Header("Material Identification")]
    public string materialId = "MAT_DEFAULT";
    public string materialDisplayName = "Untreated Drywall";

    [Header("Broadband Absorption Coefficients (0.0 to 1.0)")]
    [Tooltip("125 Hz - Low frequency bass absorption")]
    [Range(0f, 1f)] public float lowFreqAbsorption = 0.05f;

    [Tooltip("1000 Hz - Mid frequency vocal/dialogue absorption")]
    [Range(0f, 1f)] public float midFreqAbsorption = 0.05f;

    [Tooltip("4000 Hz - High frequency treble absorption")]
    [Range(0f, 1f)] public float highFreqAbsorption = 0.05f;

    [Tooltip("Scattering coefficient (diffusion rate, 0.0 = specular, 1.0 = diffuse)")]
    [Range(0f, 1f)] public float scattering = 0.05f;

    [Header("Standard Octave Band Absorption (125, 250, 500, 1000, 2000, 4000 Hz)")]
    public float[] octaveBandAbsorption = new float[6] { 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f };

    [Header("Visual Representation")]
    public Material visualMaterial;
    [Range(0f, 1f)] public float visualRoughness = 0.5f;
    [Range(0f, 1f)] public float visualMetallic = 0.0f;
    public Color visualColor = Color.white;

    [Header("Scientific Provenance & Documentation")]
    [TextArea(2, 4)]
    public string provenance = "CEDIA/CTA-RP22 Table 4 / Cox & D'Antonio Acoustic Absorbers and Diffusers";

    private void OnValidate()
    {
        lowFreqAbsorption = Mathf.Clamp01(lowFreqAbsorption);
        midFreqAbsorption = Mathf.Clamp01(midFreqAbsorption);
        highFreqAbsorption = Mathf.Clamp01(highFreqAbsorption);
        scattering = Mathf.Clamp01(scattering);

        if (octaveBandAbsorption == null || octaveBandAbsorption.Length != 6)
        {
            octaveBandAbsorption = new float[6];
        }

        for (int i = 0; i < octaveBandAbsorption.Length; i++)
        {
            octaveBandAbsorption[i] = Mathf.Clamp01(octaveBandAbsorption[i]);
        }
    }

    /// <summary>
    /// Returns absorption coefficient for a specific octave band index (0 = 125Hz ... 5 = 4kHz).
    /// </summary>
    public float GetAbsorptionAtBand(int bandIndex)
    {
        if (octaveBandAbsorption != null && bandIndex >= 0 && bandIndex < octaveBandAbsorption.Length)
        {
            return octaveBandAbsorption[bandIndex];
        }

        if (bandIndex <= 0) return lowFreqAbsorption;
        if (bandIndex >= 4) return highFreqAbsorption;
        return midFreqAbsorption;
    }

    /// <summary>
    /// Computes the arithmetic mean absorption across all 6 octave bands.
    /// </summary>
    public float GetAverageAbsorption()
    {
        if (octaveBandAbsorption != null && octaveBandAbsorption.Length == 6)
        {
            float sum = 0f;
            for (int i = 0; i < 6; i++) sum += octaveBandAbsorption[i];
            return sum / 6.0f;
        }
        return (lowFreqAbsorption + midFreqAbsorption + highFreqAbsorption) / 3.0f;
    }
}
