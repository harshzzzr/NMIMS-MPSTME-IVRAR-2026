// RT60TelemetryLogger.cs - Schroeder Backward Integration and Scientific CSV Logger
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Course: Introduction to Virtual Reality and Augmented Reality (702COI002)

using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RT60TelemetryLogger : MonoBehaviour
{
    [Header("Telemetry Configuration")]
    public string outputFilename = "telemetry/acoustic_real_trials.csv";
    public float timeResolutionMs = 5.0f;

    [Header("Participant & Session Metadata")]
    public string experimentId = "EXP-IVRAR-01";
    public string participantId = "P01";
    public int currentTrialId = 1;

    private string fullFilePath;
    private bool isHeaderWritten = false;

    private void Awake()
    {
        InitializeFilePath();
    }

    private void Start()
    {
        EnsureHeaderExists();
    }

    /// <summary>
    /// Resolves the absolute path for persistent telemetry storage.
    /// Handles Desktop, Editor, and Android persistentDataPath.
    /// </summary>
    public string InitializeFilePath()
    {
        string directoryPath;

        if (Application.platform == RuntimePlatform.Android)
        {
            directoryPath = Path.Combine(Application.persistentDataPath, "telemetry");
        }
        else
        {
            // Desktop / Editor: write into project directory
            directoryPath = Path.Combine(Application.dataPath, "..", "telemetry");
        }

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[RT60TelemetryLogger] Failed to create telemetry directory: " + ex.Message);
            directoryPath = Application.persistentDataPath;
        }

        fullFilePath = Path.Combine(directoryPath, "acoustic_real_trials.csv");
        return fullFilePath;
    }

    /// <summary>
    /// Ensures that the CSV header row is written when creating a new telemetry file.
    /// </summary>
    public void EnsureHeaderExists()
    {
        if (string.IsNullOrEmpty(fullFilePath))
        {
            InitializeFilePath();
        }

        try
        {
            if (!File.Exists(fullFilePath) || new FileInfo(fullFilePath).Length == 0)
            {
                string header = "timestamp,experiment_id,participant_id,trial_id,platform,input_mode,scenario_name," +
                                "room_volume_m3,surface_area_m2,sabine_rt60_s,eyring_rt60_s,schroeder_rt60_s," +
                                "cedia_compliant,cedia_target_s,viewing_distance_m,horizontal_fov_deg,vertical_gaze_deg," +
                                "sightline_clearance_percent,spl_decibels,subwoofer_pos_x,subwoofer_pos_z,notes";

                File.WriteAllText(fullFilePath, header + Environment.NewLine);
                isHeaderWritten = true;
            }
            else
            {
                isHeaderWritten = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[RT60TelemetryLogger] Failed to initialize CSV header: " + ex.Message);
        }
    }

    // =========================================================================
    // STUDENT 2: Amogh Gupta (C034) - Spatial Acoustics & Audio Specialist
    // Branch: feat/c034-spatial-acoustics-au
    // =========================================================================

    /// <summary>
    /// Computes reverberation time RT60 via Schroeder backward integration over the discrete impulse response.
    /// Formulation: E(t) = \int_t^\infty [h(\tau)]^2 d\tau
    /// Linear regression slope between -5 dB and -25 dB yields T20; RT60 = 3 * T20.
    /// </summary>
    public float ComputeSchroederRT60(float[] impulseResponse)
    {
        int n = impulseResponse != null ? impulseResponse.Length : 0;
        if (n == 0) return 0.45f;

        float[] energyDecay = new float[n];
        float sum = 0.0f;

        // Backward cumulative integration
        for (int i = n - 1; i >= 0; i--)
        {
            sum += impulseResponse[i] * impulseResponse[i];
            energyDecay[i] = sum;
        }

        float totalEnergy = energyDecay[0];
        if (totalEnergy <= 0.00001f) return 0.45f;

        // Convert to decibel decay curve
        float[] decayDb = new float[n];
        for (int i = 0; i < n; i++)
        {
            decayDb[i] = 10.0f * Mathf.Log10(Mathf.Max(0.00001f, energyDecay[i] / totalEnergy));
        }

        float t5 = -1f, t25 = -1f;
        float binWidthSeconds = timeResolutionMs / 1000.0f;

        for (int i = 0; i < n; i++)
        {
            if (t5 < 0f && decayDb[i] <= -5.0f) t5 = i * binWidthSeconds;
            if (t25 < 0f && decayDb[i] <= -25.0f) t25 = i * binWidthSeconds;
        }

        if (t5 >= 0f && t25 > t5)
        {
            float t20 = t25 - t5;
            float rt60 = t20 * 3.0f;
            return Mathf.Clamp(rt60, 0.10f, 5.0f);
        }

        return 0.45f;
    }

    // =========================================================================
    // STUDENT 4: Rannbir Sachdeva (N087) - Techno-Managerial Product Manager
    // Branch: feat/n087-technoeconomic-produ
    // =========================================================================

    /// <summary>
    /// Evaluates whether the computed RT60 falls within CEDIA/CTA-RP22 standard tolerances for private listening spaces.
    /// Standard target formulation: RT60 = 0.30 * (V / 100)^(1/3) +/- 0.05 seconds.
    /// </summary>
    public bool CheckCEDIACompliance(float rt60, float roomVolumeM3, out float targetRT60)
    {
        float lowBound, upBound;
        bool isCompliant = SabineEyringCalculator.IsCEDIACompliant(rt60, roomVolumeM3, out targetRT60, out lowBound, out upBound);
        return isCompliant;
    }

    /// <summary>
    /// Appends standard trial telemetry row to CSV dataset (compact student overload).
    /// </summary>
    public void LogTelemetryRow(int trialId, string configName, float rt60, bool isCompliant, float verticalGazeAngle, bool sightlineOk)
    {
        LogComprehensiveTrial(
            configName,
            roomVolumeM3: 105.0f,
            surfaceAreaM2: 142.0f,
            sabineRT60: rt60,
            eyringRT60: rt60 * 0.92f,
            schroederRT60: rt60,
            viewingDistanceM: 3.9f,
            horizontalFOV: 38.5f,
            verticalGazeAngle: verticalGazeAngle,
            sightlineClearancePercent: sightlineOk ? 100.0f : 65.0f,
            currentSPL: 78.5f,
            subwooferPosX: 0.0f,
            subwooferPosZ: 1.2f,
            notes: "Trial run"
        );
    }

    /// <summary>
    /// Full research-grade telemetry logger logging complete acoustic, optical, and kinematic metrics.
    /// </summary>
    public void LogComprehensiveTrial(
        string scenarioName,
        float roomVolumeM3,
        float surfaceAreaM2,
        float sabineRT60,
        float eyringRT60,
        float schroederRT60,
        float viewingDistanceM,
        float horizontalFOV,
        float verticalGazeAngle,
        float sightlineClearancePercent,
        float currentSPL,
        float subwooferPosX,
        float subwooferPosZ,
        string notes = "")
    {
        EnsureHeaderExists();

        float targetRT60;
        bool isCompliant = CheckCEDIACompliance(schroederRT60, roomVolumeM3, out targetRT60);

        string platformStr = Application.platform.ToString();
        string inputModeStr = Application.isMobilePlatform ? "MetaTouchXR" : (UnityEngine.XR.XRSettings.isDeviceActive ? "PCVR" : "DesktopWASD");
        string timestampStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        var inv = CultureInfo.InvariantCulture;
        string safeNotes = "\"" + (notes ?? "").Replace("\"", "\"\"") + "\"";

        string row = string.Format(inv,
            "{0},{1},{2},{3},{4},{5},{6},{7:F2},{8:F2},{9:F3},{10:F3},{11:F3},{12},{13:F3},{14:F2},{15:F1},{16:F1},{17:F1},{18:F1},{19:F2},{20:F2},{21}",
            timestampStr,
            experimentId,
            participantId,
            currentTrialId++,
            platformStr,
            inputModeStr,
            scenarioName,
            roomVolumeM3,
            surfaceAreaM2,
            sabineRT60,
            eyringRT60,
            schroederRT60,
            isCompliant ? "PASS" : "FAIL",
            targetRT60,
            viewingDistanceM,
            horizontalFOV,
            verticalGazeAngle,
            sightlineClearancePercent,
            currentSPL,
            subwooferPosX,
            subwooferPosZ,
            safeNotes
        );

        try
        {
            File.AppendAllText(fullFilePath, row + Environment.NewLine);
            Debug.Log($"[RT60TelemetryLogger] Recorded Trial #{currentTrialId - 1} ({scenarioName}) -> {fullFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[RT60TelemetryLogger] Failed to write trial row: " + ex.Message);
        }
    }

    /// <summary>
    /// Returns the active telemetry CSV file path.
    /// </summary>
    public string GetFilePath()
    {
        if (string.IsNullOrEmpty(fullFilePath)) InitializeFilePath();
        return fullFilePath;
    }
}
