// HomeCinemaSceneBuilder.cs - Automated Construction & Configuration of the IVRAR Home Cinema
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)

#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Video;

public static class HomeCinemaSceneBuilder
{
    [MenuItem("IVRAR/Build Complete Home Cinema Environment", false, 1)]
    public static void BuildHomeCinemaScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Home Cinema Environment");

        Debug.Log("[HomeCinemaSceneBuilder] Starting home cinema construction in: " + scene.name);

        // 1. Root Cinema Container
        GameObject cinemaRoot = GameObject.Find("Cinema_Environment");
        if (cinemaRoot == null)
        {
            cinemaRoot = new GameObject("Cinema_Environment");
            Undo.RegisterCreatedObjectUndo(cinemaRoot, "Create Cinema_Environment");
        }

        // Clean out default Main Camera to avoid dual camera conflicts
        GameObject defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null && defaultCam.transform.parent != cinemaRoot.transform)
        {
            Undo.DestroyObjectImmediate(defaultCam);
        }

        // 2. Materials Setup
        Material drywallMat = CreateOrGetMaterial("Mat_Drywall_Untreated", new Color(0.88f, 0.88f, 0.86f), 0.6f);
        Material concreteMat = CreateOrGetMaterial("Mat_Concrete_Polished", new Color(0.55f, 0.55f, 0.58f), 0.3f);
        Material glassMat = CreateOrGetMaterial("Mat_Glass_Facade", new Color(0.2f, 0.4f, 0.5f, 0.6f), 0.05f);
        Material hardwoodMat = CreateOrGetMaterial("Mat_Hardwood_Polished", new Color(0.52f, 0.32f, 0.16f), 0.25f);
        Material ceilingMat = CreateOrGetMaterial("Mat_Ceiling_Plaster", new Color(0.92f, 0.92f, 0.92f), 0.7f);
        Material screenFrameMat = CreateOrGetMaterial("Mat_Screen_Bezel", new Color(0.08f, 0.08f, 0.08f), 0.8f);
        Material speakerMat = CreateOrGetMaterial("Mat_Speaker_Cabinet", new Color(0.12f, 0.12f, 0.13f), 0.4f);
        Material seatMat = CreateOrGetMaterial("Mat_Cinema_Seat_Fabric", new Color(0.45f, 0.12f, 0.14f), 0.85f);

        // 3. Room Boundaries (5m x 7m x 3m)
        // Origin (0,0,0) is at floor center.
        // X: -2.5m (West Glass) to +2.5m (East Concrete) -> Width = 5.0m
        // Z: -3.5m (South Drywall) to +3.5m (North Drywall/Screen) -> Length = 7.0m
        // Y: 0.0m (Floor) to 3.0m (Ceiling) -> Height = 3.0m

        GameObject roomObj = GetOrCreateChild(cinemaRoot, "Room_Architecture");

        // North Wall (Front): 5m wide x 3m high, centered at (0, 1.5, 3.5)
        var northWall = CreateWall(roomObj, "Wall_North_Front", new Vector3(0f, 1.5f, 3.5f), new Vector3(5.0f, 3.0f, 0.1f), drywallMat, 15.0f);

        // South Wall (Rear): 5m wide x 3m high, centered at (0, 1.5, -3.5)
        var southWall = CreateWall(roomObj, "Wall_South_Rear", new Vector3(0f, 1.5f, -3.5f), new Vector3(5.0f, 3.0f, 0.1f), drywallMat, 15.0f);

        // East Wall (Left): 7m long x 3m high, centered at (2.5, 1.5, 0)
        var eastWall = CreateWall(roomObj, "Wall_East_Left_Concrete", new Vector3(2.5f, 1.5f, 0f), new Vector3(0.1f, 3.0f, 7.0f), concreteMat, 21.0f);

        // West Wall (Right): 7m long x 3m high, centered at (-2.5, 1.5, 0)
        var westWall = CreateWall(roomObj, "Wall_West_Right_Glass", new Vector3(-2.5f, 1.5f, 0f), new Vector3(0.1f, 3.0f, 7.0f), glassMat, 21.0f);

        // Floor: 5m wide x 7m long, centered at (0, -0.05, 0)
        var floor = CreateWall(roomObj, "Floor_Hardwood", new Vector3(0f, -0.05f, 0f), new Vector3(5.0f, 0.1f, 7.0f), hardwoodMat, 35.0f);

        // Ceiling: 5m wide x 7m long, centered at (0, 3.05, 0)
        var ceiling = CreateWall(roomObj, "Ceiling_Plaster", new Vector3(0f, 3.05f, 0f), new Vector3(5.0f, 0.1f, 7.0f), ceilingMat, 35.0f);

        // 4. Cinema Screen & Frame
        // Mounted on North wall at Z = 3.44m, Y = 1.6m. Size: 3.6m x 2.025m (16:9)
        GameObject stageObj = GetOrCreateChild(cinemaRoot, "Screen_Stage");
        GameObject frame = CreatePrimitive(stageObj, "Screen_Frame", PrimitiveType.Cube, new Vector3(0f, 1.6f, 3.44f), new Vector3(3.8f, 2.2f, 0.05f), screenFrameMat);

        GameObject screen = CreatePrimitive(stageObj, "Cinema_Screen", PrimitiveType.Quad, new Vector3(0f, 1.6f, 3.41f), new Vector3(3.6f, 2.025f, 1.0f), screenFrameMat);
        screen.transform.localRotation = Quaternion.identity; // Face into room towards -Z (Quad normal is -Z)
        var screenRenderer = screen.GetComponent<MeshRenderer>();

        var moviePlayer = screen.GetComponent<MovieScreenPlayer>();
        if (moviePlayer == null) moviePlayer = screen.AddComponent<MovieScreenPlayer>();
        moviePlayer.cinemaScreenRenderer = screenRenderer;

        // 5. Projector Asset
        GameObject projectorObj = CreatePrimitive(cinemaRoot, "Ceiling_Projector", PrimitiveType.Cube, new Vector3(0f, 2.7f, -2.8f), new Vector3(0.5f, 0.25f, 0.6f), speakerMat);
        Light projLight = projectorObj.GetComponent<Light>();
        if (projLight == null) projLight = projectorObj.AddComponent<Light>();
        projLight.type = LightType.Spot;
        projLight.spotAngle = 45f;
        projLight.range = 8.0f;
        projLight.intensity = 1.5f;
        projLight.color = new Color(0.95f, 0.95f, 1.0f);
        projectorObj.transform.LookAt(screen.transform.position);

        // 6. Dolby Atmos 7.1.4 Loudspeaker Layout
        GameObject audioSystemObj = GetOrCreateChild(cinemaRoot, "Audio_System_7.1.4");
        List<Transform> speakerTransforms = new List<Transform>();

        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_FL", new Vector3(-1.8f, 1.2f, 3.2f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_C", new Vector3(0.0f, 1.2f, 3.35f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_FR", new Vector3(1.8f, 1.2f, 3.2f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_SL", new Vector3(-2.35f, 1.5f, 0.0f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_SR", new Vector3(2.35f, 1.5f, 0.0f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_RL", new Vector3(-1.8f, 1.5f, -3.2f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_RR", new Vector3(1.8f, 1.5f, -3.2f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_TFL", new Vector3(-1.3f, 2.85f, 1.5f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_TFR", new Vector3(1.3f, 2.85f, 1.5f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_TRL", new Vector3(-1.3f, 2.85f, -1.5f), screenFrameMat));
        speakerTransforms.Add(CreateSpeaker(audioSystemObj, "Speaker_TRR", new Vector3(1.3f, 2.85f, -1.5f), screenFrameMat));

        // Subwoofer (LFE)
        GameObject subObj = CreatePrimitive(audioSystemObj, "Subwoofer_LFE", PrimitiveType.Cube, new Vector3(2.0f, 0.25f, 2.9f), new Vector3(0.55f, 0.5f, 0.55f), speakerMat);
        var subController = subObj.GetComponent<SubwooferController>();
        if (subController == null) subController = subObj.AddComponent<SubwooferController>();
        subController.minX = -2.1f;
        subController.maxX = 2.1f;
        subController.minZ = -3.1f;
        subController.maxZ = 3.1f;
        subController.floorY = 0.25f;

        // 7. Seating Geometry
        GameObject seatingObj = GetOrCreateChild(cinemaRoot, "Seating_Arrangement");
        GameObject primaryRow = CreateSeatingRow(seatingObj, "Primary_Seating_Row", new Vector3(0f, 0.0f, -0.4f), seatMat, 3);
        GameObject secondaryRow = CreateSeatingRow(seatingObj, "Secondary_Seating_Row", new Vector3(0f, 0.35f, -1.8f), seatMat, 4);

        Transform primaryEyePoint = new GameObject("Primary_Listening_Position_Eyes").transform;
        primaryEyePoint.SetParent(primaryRow.transform);
        primaryEyePoint.localPosition = new Vector3(0f, 1.15f, 0f);

        Transform secondaryEyePoint = new GameObject("Secondary_Listening_Position_Eyes").transform;
        secondaryEyePoint.SetParent(secondaryRow.transform);
        secondaryEyePoint.localPosition = new Vector3(0f, 1.15f, 0f);

        // 8. Management & Core Simulation Scripts
        GameObject simManager = GetOrCreateChild(cinemaRoot, "Simulation_Services");

        var splMeter = simManager.GetComponent<VirtualSPLMeter>();
        if (splMeter == null) splMeter = simManager.AddComponent<VirtualSPLMeter>();
        splMeter.primaryListeningPosition = primaryEyePoint;
        splMeter.secondaryListeningPosition = secondaryEyePoint;

        var optics = simManager.GetComponent<OpticsErgonomicsCalculator>();
        if (optics == null) optics = simManager.AddComponent<OpticsErgonomicsCalculator>();
        optics.playerEyes = primaryEyePoint;
        optics.movieScreen = screen.transform;

        var raycaster = simManager.GetComponent<AcousticRaycaster>();
        if (raycaster == null) raycaster = simManager.AddComponent<AcousticRaycaster>();
        raycaster.primaryListeningPosition = primaryEyePoint;
        raycaster.loudspeakerSources = speakerTransforms.ToArray();

        var telemetry = simManager.GetComponent<RT60TelemetryLogger>();
        if (telemetry == null) telemetry = simManager.AddComponent<RT60TelemetryLogger>();
        raycaster.telemetryLogger = telemetry;

        var env = simManager.GetComponent<EnvironmentModifier>();
        if (env == null) env = simManager.AddComponent<EnvironmentModifier>();
        env.northWall = northWall;
        env.southWall = southWall;
        env.eastWall = eastWall;
        env.westWall = westWall;
        env.floorSurface = floor;
        env.ceilingSurface = ceiling;
        env.subwoofer = subController;
        env.primarySeatingRow = primaryRow.transform;
        env.secondarySeatingRow = secondaryRow.transform;
        env.opticsCalculator = optics;
        env.raycaster = raycaster;
        env.telemetryLogger = telemetry;
        env.splMeter = splMeter;

        // 9. Camera Rigs (Desktop & Meta XR)
        GameObject rigsParent = GetOrCreateChild(cinemaRoot, "XR_and_Desktop_Rigs");

        // Desktop Rig
        GameObject desktopRig = GetOrCreateChild(rigsParent, "Desktop_FPS_Rig");
        desktopRig.transform.position = new Vector3(0f, 1.15f, -0.4f);
        GameObject desktopCamObj = GetOrCreateChild(desktopRig, "Desktop_Camera");
        var dCam = desktopCamObj.GetComponent<Camera>();
        if (dCam == null) dCam = desktopCamObj.AddComponent<Camera>();
        dCam.nearClipPlane = 0.1f;
        dCam.farClipPlane = 100f;
        desktopCamObj.tag = "MainCamera";
        if (desktopCamObj.GetComponent<AudioListener>() == null) desktopCamObj.AddComponent<AudioListener>();

        var desktopController = desktopRig.GetComponent<CrossPlatformInputController>();
        if (desktopController == null) desktopController = desktopRig.AddComponent<CrossPlatformInputController>();
        desktopController.playerCamera = desktopCamObj.transform;
        desktopController.environmentModifier = env;
        desktopController.moviePlayer = moviePlayer;
        desktopController.raycaster = raycaster;
        desktopController.telemetryLogger = telemetry;

        // Meta XR Rig Instance
        GameObject metaXRRigInstance = SetupMetaXRRig(rigsParent);

        // Platform Manager
        var platformManager = simManager.GetComponent<PlatformManager>();
        if (platformManager == null) platformManager = simManager.AddComponent<PlatformManager>();
        platformManager.desktopRig = desktopRig;
        platformManager.metaXRRig = metaXRRigInstance;
        platformManager.spawnPosition = new Vector3(0f, 1.15f, -0.4f);

        // 10. Dashboard UI
        var dashboard = simManager.GetComponent<CinemaUIDashboard>();
        if (dashboard == null) dashboard = simManager.AddComponent<CinemaUIDashboard>();
        dashboard.environmentModifier = env;
        dashboard.opticsCalculator = optics;
        dashboard.splMeter = splMeter;
        dashboard.subwoofer = subController;
        dashboard.moviePlayer = moviePlayer;
        dashboard.raycaster = raycaster;
        dashboard.telemetryLogger = telemetry;
        dashboard.platformManager = platformManager;

        // 11. Soft Theater Sconce Lighting
        CreateSconce(cinemaRoot, "Sconce_Left_1", new Vector3(2.4f, 2.0f, 1.5f));
        CreateSconce(cinemaRoot, "Sconce_Left_2", new Vector3(2.4f, 2.0f, -1.5f));
        CreateSconce(cinemaRoot, "Sconce_Right_1", new Vector3(-2.4f, 2.0f, 1.5f));
        CreateSconce(cinemaRoot, "Sconce_Right_2", new Vector3(-2.4f, 2.0f, -1.5f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[HomeCinemaSceneBuilder] Home Cinema Environment successfully constructed and saved!");
    }

    private static GameObject SetupMetaXRRig(GameObject parent)
    {
        // Try finding official OVRCameraRig prefab from Meta XR Core SDK
        string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
        GameObject ovrPrefab = null;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("OVRCameraRig.prefab") && !path.Contains("Sample"))
            {
                ovrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                break;
            }
        }

        GameObject rigObj;
        if (ovrPrefab != null)
        {
            rigObj = (GameObject)PrefabUtility.InstantiatePrefab(ovrPrefab, parent.transform);
            rigObj.name = "Meta_OVRCameraRig";
            Debug.Log("[HomeCinemaSceneBuilder] Instantiated official Meta OVRCameraRig prefab.");
        }
        else
        {
            rigObj = GetOrCreateChild(parent, "Meta_OVRCameraRig");
            Debug.LogWarning("[HomeCinemaSceneBuilder] Official OVRCameraRig prefab not found in search, created container.");
        }

        rigObj.transform.position = new Vector3(0f, 0f, -0.4f);
        return rigObj;
    }

    private static AcousticSurfaceProperties CreateWall(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, float surfaceArea)
    {
        GameObject wall = CreatePrimitive(parent, name, PrimitiveType.Cube, pos, scale, mat);
        var prop = wall.GetComponent<AcousticSurfaceProperties>();
        if (prop == null) prop = wall.AddComponent<AcousticSurfaceProperties>();
        prop.surfaceName = name;
        prop.surfaceAreaM2 = surfaceArea;
        prop.targetRenderer = wall.GetComponent<MeshRenderer>();
        return prop;
    }

    private static Transform CreateSpeaker(GameObject parent, string name, Vector3 pos, Material mat)
    {
        GameObject spk = CreatePrimitive(parent, name, PrimitiveType.Cube, pos, new Vector3(0.28f, 0.42f, 0.22f), mat);
        var aSrc = spk.GetComponent<AudioSource>();
        if (aSrc == null) aSrc = spk.AddComponent<AudioSource>();
        aSrc.spatialBlend = 1.0f; // 3D spatialized
        aSrc.playOnAwake = false;
        aSrc.minDistance = 1.0f;
        aSrc.maxDistance = 15.0f;
        return spk.transform;
    }

    private static GameObject CreateSeatingRow(GameObject parent, string name, Vector3 pos, Material mat, int seatCount)
    {
        GameObject row = GetOrCreateChild(parent, name);
        row.transform.position = pos;

        float seatWidth = 0.65f;
        float startX = -(seatCount - 1) * seatWidth * 0.5f;

        for (int i = 0; i < seatCount; i++)
        {
            Vector3 seatPos = new Vector3(startX + i * seatWidth, 0.25f, 0f);
            GameObject seat = CreatePrimitive(row, "Seat_" + (i + 1), PrimitiveType.Cube, seatPos, new Vector3(0.55f, 0.5f, 0.6f), mat);
            // Seat back
            CreatePrimitive(seat, "Backrest", PrimitiveType.Cube, new Vector3(0f, 0.35f, -0.25f), new Vector3(0.55f, 0.6f, 0.12f), mat);
        }

        return row;
    }

    private static void CreateSconce(GameObject parent, string name, Vector3 pos)
    {
        GameObject sconce = CreatePrimitive(parent, name, PrimitiveType.Cylinder, pos, new Vector3(0.12f, 0.25f, 0.12f), null);
        Light l = sconce.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 3.5f;
        l.intensity = 0.4f;
        l.color = new Color(1.0f, 0.85f, 0.65f); // Warm theater amber
    }

    private static GameObject CreatePrimitive(GameObject parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject obj = GetOrCreateChild(parent, name);
        var mf = obj.GetComponent<MeshFilter>();
        if (mf == null)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            var mr = obj.AddComponent<MeshRenderer>();
            var col = obj.AddComponent<BoxCollider>();
            UnityEngine.Object.DestroyImmediate(temp);
        }

        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;

        if (mat != null)
        {
            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        return obj;
    }

    private static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null) return child.gameObject;

        GameObject newObj = new GameObject(name);
        newObj.transform.SetParent(parent.transform, false);
        return newObj;
    }

    private static Material CreateOrGetMaterial(string name, Color color, float roughness)
    {
        string dir = "Assets/_Project/Materials";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string path = $"{dir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            mat = new Material(urpLit);
            mat.name = name;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1.0f - roughness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);

            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}
#endif
