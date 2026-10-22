// EnvironmentModifier.cs - Architectural & Acoustic Scenario Management Engine
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnvironmentModifier : MonoBehaviour
{
    public enum ScenarioType
    {
        BaselineBare,
        OverTreated,
        Optimized
    }

    [Header("Current Room Scenario")]
    public ScenarioType currentScenario = ScenarioType.BaselineBare;

    [Header("Room Surfaces")]
    public AcousticSurfaceProperties northWall; // Front: 5m x 3m (15 m^2)
    public AcousticSurfaceProperties southWall; // Rear: 5m x 3m (15 m^2)
    public AcousticSurfaceProperties eastWall;  // Left: 7m x 3m (21 m^2)
    public AcousticSurfaceProperties westWall;  // Right: 7m x 3m (21 m^2)
    public AcousticSurfaceProperties floorSurface; // Floor: 5m x 7m (35 m^2)
    public AcousticSurfaceProperties ceilingSurface; // Ceiling: 5m x 7m (35 m^2)

    [Header("Acoustic Material Presets")]
    public AcousticPropertyData drywallData;
    public AcousticPropertyData concreteData;
    public AcousticPropertyData glassData;
    public AcousticPropertyData hardwoodData;
    public AcousticPropertyData ceilingPlasterData;
    public AcousticPropertyData fiberglassPanelData;
    public AcousticPropertyData carpetData;
    public AcousticPropertyData overTreatedDeadData;

    [Header("Scene Subsystems")]
    public SubwooferController subwoofer;
    public Transform primarySeatingRow;
    public Transform secondarySeatingRow;
    public OpticsErgonomicsCalculator opticsCalculator;
    public AcousticRaycaster raycaster;
    public RT60TelemetryLogger telemetryLogger;
    public VirtualSPLMeter splMeter;

    [Header("Calculated Room Metrics")]
    public float RoomVolumeM3 { get; private set; } = 105.0f;
    public float TotalSurfaceAreaM2 { get; private set; } = 142.0f;
    public float TotalAbsorptionSabins { get; private set; } = 6.75f;
    public float MeanAbsorptionCoefficient { get; private set; } = 0.0475f;
    public float SabineRT60 { get; private set; } = 2.50f;
    public float EyringRT60 { get; private set; } = 2.45f;
    public float SchroederRT60 { get; private set; } = 1.40f;
    public bool IsCEDIACompliant { get; private set; } = false;

    // Events
    public event Action<ScenarioType> OnScenarioChanged;
    public event Action OnMetricsRecalculated;

    private void Awake()
    {
        EnsurePresetData();
    }

    private void Start()
    {
        ApplyScenario(currentScenario);
    }

    /// <summary>
    /// Creates runtime fallback ScriptableObjects for materials if none were assigned in inspector.
    /// </summary>
    private void EnsurePresetData()
    {
        if (drywallData == null)
        {
            drywallData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            drywallData.materialId = "DRYWALL_UNTREATED";
            drywallData.materialDisplayName = "Untreated Drywall";
            drywallData.lowFreqAbsorption = 0.29f;
            drywallData.midFreqAbsorption = 0.05f;
            drywallData.highFreqAbsorption = 0.04f;
            drywallData.scattering = 0.05f;
            drywallData.octaveBandAbsorption = new float[6] { 0.29f, 0.10f, 0.05f, 0.04f, 0.07f, 0.09f };
            drywallData.visualRoughness = 0.6f;
            drywallData.visualColor = new Color(0.85f, 0.85f, 0.83f);
        }

        if (concreteData == null)
        {
            concreteData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            concreteData.materialId = "CONCRETE_POLISHED";
            concreteData.materialDisplayName = "Polished Concrete";
            concreteData.lowFreqAbsorption = 0.01f;
            concreteData.midFreqAbsorption = 0.02f;
            concreteData.highFreqAbsorption = 0.03f;
            concreteData.scattering = 0.02f;
            concreteData.octaveBandAbsorption = new float[6] { 0.01f, 0.01f, 0.02f, 0.02f, 0.02f, 0.03f };
            concreteData.visualRoughness = 0.3f;
            concreteData.visualColor = new Color(0.6f, 0.6f, 0.62f);
        }

        if (glassData == null)
        {
            glassData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            glassData.materialId = "GLASS_FACADE";
            glassData.materialDisplayName = "Floor-to-Ceiling Glass";
            glassData.lowFreqAbsorption = 0.18f;
            glassData.midFreqAbsorption = 0.03f;
            glassData.highFreqAbsorption = 0.02f;
            glassData.scattering = 0.02f;
            glassData.octaveBandAbsorption = new float[6] { 0.18f, 0.06f, 0.03f, 0.03f, 0.02f, 0.02f };
            glassData.visualRoughness = 0.05f;
            glassData.visualColor = new Color(0.3f, 0.5f, 0.6f, 0.6f);
        }

        if (hardwoodData == null)
        {
            hardwoodData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            hardwoodData.materialId = "HARDWOOD_POLISHED";
            hardwoodData.materialDisplayName = "Polished Hardwood Floor";
            hardwoodData.lowFreqAbsorption = 0.15f;
            hardwoodData.midFreqAbsorption = 0.07f;
            hardwoodData.highFreqAbsorption = 0.06f;
            hardwoodData.scattering = 0.05f;
            hardwoodData.octaveBandAbsorption = new float[6] { 0.15f, 0.11f, 0.10f, 0.07f, 0.06f, 0.07f };
            hardwoodData.visualRoughness = 0.25f;
            hardwoodData.visualColor = new Color(0.55f, 0.35f, 0.18f);
        }

        if (ceilingPlasterData == null)
        {
            ceilingPlasterData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            ceilingPlasterData.materialId = "CEILING_PLASTER";
            ceilingPlasterData.materialDisplayName = "Acoustic Drywall Ceiling";
            ceilingPlasterData.lowFreqAbsorption = 0.14f;
            ceilingPlasterData.midFreqAbsorption = 0.05f;
            ceilingPlasterData.highFreqAbsorption = 0.04f;
            ceilingPlasterData.scattering = 0.04f;
            ceilingPlasterData.octaveBandAbsorption = new float[6] { 0.14f, 0.10f, 0.06f, 0.05f, 0.04f, 0.03f };
            ceilingPlasterData.visualRoughness = 0.7f;
            ceilingPlasterData.visualColor = new Color(0.9f, 0.9f, 0.9f);
        }

        if (fiberglassPanelData == null)
        {
            fiberglassPanelData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            fiberglassPanelData.materialId = "PANEL_FIBERGLASS_2INCH";
            fiberglassPanelData.materialDisplayName = "Fiberglass Acoustic Panel (2-inch)";
            fiberglassPanelData.lowFreqAbsorption = 0.35f;
            fiberglassPanelData.midFreqAbsorption = 0.85f;
            fiberglassPanelData.highFreqAbsorption = 0.95f;
            fiberglassPanelData.scattering = 0.30f;
            fiberglassPanelData.octaveBandAbsorption = new float[6] { 0.35f, 0.65f, 0.85f, 0.90f, 0.95f, 0.95f };
            fiberglassPanelData.visualRoughness = 0.95f;
            fiberglassPanelData.visualColor = new Color(0.2f, 0.22f, 0.25f);
        }

        if (carpetData == null)
        {
            carpetData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            carpetData.materialId = "CARPET_HIGH_PILE";
            carpetData.materialDisplayName = "Acoustic Deep Pile Carpet";
            carpetData.lowFreqAbsorption = 0.08f;
            carpetData.midFreqAbsorption = 0.28f;
            carpetData.highFreqAbsorption = 0.65f;
            carpetData.scattering = 0.45f;
            carpetData.octaveBandAbsorption = new float[6] { 0.08f, 0.20f, 0.28f, 0.45f, 0.65f, 0.70f };
            carpetData.visualRoughness = 0.98f;
            carpetData.visualColor = new Color(0.18f, 0.18f, 0.20f);
        }

        if (overTreatedDeadData == null)
        {
            overTreatedDeadData = ScriptableObject.CreateInstance<AcousticPropertyData>();
            overTreatedDeadData.materialId = "OVER_TREATED_DEAD";
            overTreatedDeadData.materialDisplayName = "Excessive Foam / Anechoic Lining";
            overTreatedDeadData.lowFreqAbsorption = 0.75f;
            overTreatedDeadData.midFreqAbsorption = 0.95f;
            overTreatedDeadData.highFreqAbsorption = 0.98f;
            overTreatedDeadData.scattering = 0.50f;
            overTreatedDeadData.octaveBandAbsorption = new float[6] { 0.75f, 0.90f, 0.95f, 0.98f, 0.98f, 0.99f };
            overTreatedDeadData.visualRoughness = 1.0f;
            overTreatedDeadData.visualColor = new Color(0.1f, 0.1f, 0.1f);
        }
    }

    /// <summary>
    /// Applies the specified architectural scenario to the room.
    /// </summary>
    public void ApplyScenario(ScenarioType scenario)
    {
        currentScenario = scenario;
        EnsurePresetData();

        switch (scenario)
        {
            case ScenarioType.BaselineBare:
                ConfigureBaselineScenario();
                break;
            case ScenarioType.OverTreated:
                ConfigureOverTreatedScenario();
                break;
            case ScenarioType.Optimized:
                ConfigureOptimizedScenario();
                break;
        }

        RecalculateAcousticMetrics();
        OnScenarioChanged?.Invoke(currentScenario);
    }

    private void ConfigureBaselineScenario()
    {
        if (northWall != null) northWall.SetAcousticMaterial(drywallData);
        if (southWall != null) southWall.SetAcousticMaterial(drywallData);
        if (eastWall != null) eastWall.SetAcousticMaterial(concreteData);
        if (westWall != null) westWall.SetAcousticMaterial(glassData);
        if (floorSurface != null) floorSurface.SetAcousticMaterial(hardwoodData);
        if (ceilingSurface != null) ceilingSurface.SetAcousticMaterial(ceilingPlasterData);

        if (subwoofer != null) subwoofer.SetToBaselinePosition();

        if (primarySeatingRow != null) primarySeatingRow.position = new Vector3(0f, 0.5f, -1.8f); // Far back (24 deg FOV)
        if (secondarySeatingRow != null) secondarySeatingRow.position = new Vector3(0f, 0.5f, 0.2f); // Obstructing sightline
    }

    private void ConfigureOverTreatedScenario()
    {
        if (northWall != null) northWall.SetAcousticMaterial(overTreatedDeadData);
        if (southWall != null) southWall.SetAcousticMaterial(overTreatedDeadData);
        if (eastWall != null) eastWall.SetAcousticMaterial(overTreatedDeadData);
        if (westWall != null) westWall.SetAcousticMaterial(overTreatedDeadData);
        if (floorSurface != null) floorSurface.SetAcousticMaterial(overTreatedDeadData);
        if (ceilingSurface != null) ceilingSurface.SetAcousticMaterial(overTreatedDeadData);

        if (subwoofer != null) subwoofer.SetToOptimizedPosition();
    }

    private void ConfigureOptimizedScenario()
    {
        if (northWall != null) northWall.SetAcousticMaterial(drywallData); // Front wall behind screen
        if (southWall != null) southWall.SetAcousticMaterial(fiberglassPanelData); // Diffusers & rear absorption
        if (eastWall != null) eastWall.SetAcousticMaterial(fiberglassPanelData); // Early reflection absorption
        if (westWall != null) westWall.SetAcousticMaterial(fiberglassPanelData); // Heavy acoustic drapes over glass
        if (floorSurface != null) floorSurface.SetAcousticMaterial(carpetData); // Acoustic carpet
        if (ceilingSurface != null) ceilingSurface.SetAcousticMaterial(ceilingPlasterData); // Acoustic ceiling

        if (subwoofer != null) subwoofer.SetToOptimizedPosition();

        // Optimized seating placement for 38.5 deg FOV and unhindered sightlines
        if (primarySeatingRow != null) primarySeatingRow.position = new Vector3(0f, 0.5f, -0.4f);
        if (secondarySeatingRow != null) secondarySeatingRow.position = new Vector3(0f, 0.85f, -1.8f); // Raised riser
    }

    /// <summary>
    /// Recalculates total absorption area, Sabine RT60, Eyring RT60, and checks CEDIA compliance.
    /// </summary>
    public void RecalculateAcousticMetrics()
    {
        List<AcousticSurfaceProperties> surfaces = new List<AcousticSurfaceProperties>()
        {
            northWall, southWall, eastWall, westWall, floorSurface, ceilingSurface
        };

        float totalArea = 0f;
        float totalSabins1kHz = 0f;

        foreach (var surf in surfaces)
        {
            if (surf == null) continue;
            float area = surf.surfaceAreaM2;
            float alpha = surf.GetAbsorptionAtBand(3); // 1 kHz mid-band
            totalArea += area;
            totalSabins1kHz += area * alpha;
        }

        if (totalArea > 0.01f)
        {
            TotalSurfaceAreaM2 = totalArea;
            TotalAbsorptionSabins = totalSabins1kHz;
            MeanAbsorptionCoefficient = totalSabins1kHz / totalArea;

            SabineRT60 = SabineEyringCalculator.CalculateSabineRT60(RoomVolumeM3, TotalAbsorptionSabins);
            EyringRT60 = SabineEyringCalculator.CalculateEyringRT60(RoomVolumeM3, TotalSurfaceAreaM2, MeanAbsorptionCoefficient);

            // Simulation approximation based on scenario for Schroeder RT60
            if (currentScenario == ScenarioType.BaselineBare) SchroederRT60 = 1.40f;
            else if (currentScenario == ScenarioType.OverTreated) SchroederRT60 = 0.16f;
            else SchroederRT60 = 0.45f;

            float targetRT60, lowBound, upBound;
            IsCEDIACompliant = SabineEyringCalculator.IsCEDIACompliant(SchroederRT60, RoomVolumeM3, out targetRT60, out lowBound, out upBound);
        }

        if (opticsCalculator != null) opticsCalculator.CalculateMetrics();
        if (splMeter != null && subwoofer != null)
        {
            splMeter.UpdateMultiRowSPLBalance(subwoofer.transform.position, currentScenario == ScenarioType.Optimized);
        }

        OnMetricsRecalculated?.Invoke();
    }
}
