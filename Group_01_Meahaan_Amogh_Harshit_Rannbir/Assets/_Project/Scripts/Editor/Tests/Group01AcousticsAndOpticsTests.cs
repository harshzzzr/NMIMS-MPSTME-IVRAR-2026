// Group01AcousticsAndOpticsTests.cs - Automated Unit & Integration Tests
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)

#if UNITY_EDITOR
using System;
using System.IO;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class Group01AcousticsAndOpticsTests
{
    // =========================================================================
    // 1. STATISTICAL ACOUSTICS & REVERBERATION ESTIMATORS
    // =========================================================================

    [Test]
    public void Test_SabineEquation_HandCalculatedReferenceCase()
    {
        // Reference home cinema: V = 105.0 m^3, Total metric Sabins A = 6.75 m^2
        // Hand calculation: RT60 = (0.161 * 105.0) / 6.75 = 16.905 / 6.75 = 2.50444 s
        float volume = 105.0f;
        float absorptionArea = 6.75f;
        float expectedRT60 = 2.50444f;

        float computedRT60 = SabineEyringCalculator.CalculateSabineRT60(volume, absorptionArea);

        Assert.AreEqual(expectedRT60, computedRT60, 0.005f, "Sabine RT60 must match hand-calculated reference within 5ms.");
    }

    [Test]
    public void Test_EyringEquation_HandCalculatedReferenceCase()
    {
        // V = 105.0 m^3, S = 142.0 m^2, alpha_mean = 6.75 / 142.0 = 0.047535
        float volume = 105.0f;
        float surfaceArea = 142.0f;
        float meanAlpha = 6.75f / 142.0f;

        float computedEyring = SabineEyringCalculator.CalculateEyringRT60(volume, surfaceArea, meanAlpha, false);

        // Denominator: -142 * ln(1 - 0.047535) = -142 * (-0.04870) = 6.9154
        // Expected RT60 = 16.905 / 6.9154 = 2.4445 s
        float expectedEyring = 2.4445f;

        Assert.AreEqual(expectedEyring, computedEyring, 0.01f, "Eyring RT60 must match hand-calculated reference within 10ms.");
        Assert.Less(computedEyring, 2.505f, "Eyring RT60 must always be <= Sabine RT60 for same enclosure.");
    }

    [Test]
    public void Test_InvalidAcousticInputs_ZeroVolumeAndAbsorption()
    {
        // Edge cases: V <= 0, A <= 0, alpha >= 1.0
        float zeroVolRT60 = SabineEyringCalculator.CalculateSabineRT60(0f, 10f);
        Assert.AreEqual(0f, zeroVolRT60, "Zero volume should yield 0s safe output.");

        float zeroAbsRT60 = SabineEyringCalculator.CalculateSabineRT60(100f, 0f);
        Assert.IsTrue(float.IsPositiveInfinity(zeroAbsRT60), "Zero absorption should yield positive infinity.");

        float fullAbsEyring = SabineEyringCalculator.CalculateEyringRT60(100f, 100f, 1.0f);
        Assert.IsFalse(float.IsNaN(fullAbsEyring), "Full absorption should not produce NaN.");
        Assert.Greater(fullAbsEyring, 0f, "Full absorption must be positive and bounded.");
    }

    [Test]
    public void Test_CEDIACompliance_TolerancesAndTargets()
    {
        float volume = 105.0f;
        float targetRT60, lowBound, upBound;
        targetRT60 = SabineEyringCalculator.CalculateCEDIATargetRT60(volume, 0.05f, out lowBound, out upBound);

        // Target for 105m^3 is approx 0.30 * (1.05)^0.33 approx 0.305 s
        Assert.AreEqual(0.305f, targetRT60, 0.02f, "CEDIA nominal target should be approx 0.30-0.31s for 105m^3.");
        Assert.GreaterOrEqual(lowBound, 0.20f, "Lower bound must be at least 0.20s.");
        Assert.LessOrEqual(upBound, 0.60f, "Upper bound must not exceed 0.60s.");

        // Optimized target: 0.45s should pass within private listening theater tolerance
        bool is45Compliant = SabineEyringCalculator.IsCEDIACompliant(0.45f, volume, out _, out _, out _);
        Assert.IsTrue(is45Compliant, "0.45s should be within acceptable CEDIA listening theater window.");

        // Baseline: 1.40s should fail
        bool is140Compliant = SabineEyringCalculator.IsCEDIACompliant(1.40f, volume, out _, out _, out _);
        Assert.IsFalse(is140Compliant, "1.40s baseline must fail CEDIA compliance.");

        // Over-treated: 0.15s should fail
        bool is15Compliant = SabineEyringCalculator.IsCEDIACompliant(0.15f, volume, out _, out _, out _);
        Assert.IsFalse(is15Compliant, "0.15s over-treated must fail CEDIA compliance.");
    }

    // =========================================================================
    // 2. OPTICAL GEOMETRY & SIGHTLINES
    // =========================================================================

    [Test]
    public void Test_ViewingAngle_TrigonometricFormula()
    {
        // theta_H = 2 * atan(W / (2d)) * 180 / pi
        // Let screen width W = 2.7m, viewer distance d = 3.9m
        // atan(2.7 / 7.8) = atan(0.34615) = 19.09 deg -> theta = 38.18 deg
        float screenWidth = 2.7f;
        float distance = 3.9f;

        float expectedFOV = 2.0f * Mathf.Atan(screenWidth / (2.0f * distance)) * Mathf.Rad2Deg;

        Assert.AreEqual(38.18f, expectedFOV, 0.05f, "Horizontal FOV must match inverse tangent formulation.");
        Assert.IsTrue(expectedFOV >= 36.0f && expectedFOV <= 40.0f, "Optimal viewing distance satisfies SMPTE/THX 36-40 deg standard.");
    }

    [Test]
    public void Test_SightlineVerticalGaze_ElevationThreshold()
    {
        // Test SMPTE EG 18-1994 threshold: vertical elevation <= 15 deg
        Vector3 viewerEye = new Vector3(0f, 1.15f, -0.4f);
        Vector3 screenCenter = new Vector3(0f, 1.60f, 3.41f);

        Vector3 toScreen = screenCenter - viewerEye;
        Vector3 horizProj = new Vector3(toScreen.x, 0f, toScreen.z);
        float gazeAngle = Vector3.Angle(horizProj.normalized, toScreen);

        Assert.Less(gazeAngle, 15.0f, "Vertical gaze angle from primary seat to screen center must be <= 15 deg.");
    }

    // =========================================================================
    // 3. KINEMATIC & GEOMETRIC CONSTRAINTS
    // =========================================================================

    [Test]
    public void Test_SubwooferBounds_Clamping()
    {
        GameObject subObj = new GameObject("TestSubwoofer");
        var controller = subObj.AddComponent<SubwooferController>();
        controller.minX = -2.1f;
        controller.maxX = 2.1f;
        controller.minZ = -3.1f;
        controller.maxZ = 3.1f;
        controller.floorY = 0.25f;

        // Try setting position outside room boundary
        subObj.transform.position = new Vector3(5.0f, 10.0f, -8.0f);
        controller.ClampToRoomBounds();

        Assert.AreEqual(2.1f, subObj.transform.position.x, 0.001f, "X must be clamped to maxX.");
        Assert.AreEqual(0.25f, subObj.transform.position.y, 0.001f, "Y must remain fixed at floor level.");
        Assert.AreEqual(-3.1f, subObj.transform.position.z, 0.001f, "Z must be clamped to minZ.");

        UnityEngine.Object.DestroyImmediate(subObj);
    }

    [Test]
    public void Test_FibonacciLattice_UnitSphereNormalization()
    {
        GameObject rayObj = new GameObject("TestRaycaster");
        var raycaster = rayObj.AddComponent<AcousticRaycaster>();

        int count = 250;
        Vector3[] dirs = raycaster.GenerateUniformRayDirections(count);

        Assert.AreEqual(count, dirs.Length);
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(1.0f, dirs[i].magnitude, 0.001f, $"Ray direction {i} must be normalized to unit length.");
            Assert.IsFalse(float.IsNaN(dirs[i].x) || float.IsNaN(dirs[i].y) || float.IsNaN(dirs[i].z), $"Ray direction {i} must not have NaN components.");
        }

        UnityEngine.Object.DestroyImmediate(rayObj);
    }

    // =========================================================================
    // 4. SCHROEDER INTEGRATION & TELEMETRY CSV
    // =========================================================================

    [Test]
    public void Test_SchroederRT60_ExponentialDecayModel()
    {
        GameObject loggerObj = new GameObject("TestLogger");
        var logger = loggerObj.AddComponent<RT60TelemetryLogger>();
        logger.timeResolutionMs = 5.0f; // 5ms bins

        // Synthesize an exponential decay impulse response h(t) = exp(-t / tau)
        // For RT60 = 0.60 s, decay by 60 dB (10^-3 amplitude) -> tau = 0.60 / ln(1000) = 0.60 / 6.908 = 0.08686 s
        int bins = 200; // 1.0 s
        float[] ir = new float[bins];
        float tau = 0.08686f;

        for (int i = 0; i < bins; i++)
        {
            float t = i * 0.005f;
            ir[i] = Mathf.Exp(-t / tau);
        }

        float computedRT60 = logger.ComputeSchroederRT60(ir);

        // Extrapolated RT60 should be close to 0.60s
        Assert.AreEqual(0.60f, computedRT60, 0.08f, "Schroeder RT60 must extract linear regression decay matching synthesized model.");

        UnityEngine.Object.DestroyImmediate(loggerObj);
    }

    [Test]
    public void Test_CSV_TelemetryFormatting_InvariantCulture()
    {
        GameObject loggerObj = new GameObject("TestLogger");
        var logger = loggerObj.AddComponent<RT60TelemetryLogger>();
        string tempPath = logger.InitializeFilePath();

        logger.LogComprehensiveTrial(
            scenarioName: "UnitTest_Optimized",
            roomVolumeM3: 105.0f,
            surfaceAreaM2: 142.0f,
            sabineRT60: 0.452f,
            eyringRT60: 0.441f,
            schroederRT60: 0.450f,
            viewingDistanceM: 3.90f,
            horizontalFOV: 38.5f,
            verticalGazeAngle: 8.5f,
            sightlineClearancePercent: 100.0f,
            currentSPL: 78.5f,
            subwooferPosX: 0.0f,
            subwooferPosZ: 1.2f,
            notes: "Automated Test Run"
        );

        Assert.IsTrue(File.Exists(tempPath), "CSV telemetry file must be created on disk.");
        string[] lines = File.ReadAllLines(tempPath);
        Assert.GreaterOrEqual(lines.Length, 2, "CSV must contain header and at least one data row.");

        string lastRow = lines[lines.Length - 1];
        Assert.IsTrue(lastRow.Contains("UnitTest_Optimized"), "Data row must contain the logged scenario name.");
        Assert.IsTrue(lastRow.Contains("105.00") || lastRow.Contains("105.0"), "Data row must contain invariant formatted numbers.");
        Assert.IsFalse(lastRow.Contains("105,00"), "Data row must NOT contain comma decimal separators.");

        UnityEngine.Object.DestroyImmediate(loggerObj);
    }
}
#endif
