// BuildingBlockConfigurator.cs - Official Meta XR Building Blocks Scene Registration
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)
// Automatically configures Camera Rig, Controller Tracking, Controller Buttons Mapper,
// and the Interactive Home Theater Media Selector Console.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using Meta.XR.BuildingBlocks;

public static class BuildingBlockConfigurator
{
    private const string CameraRigBlockId = "e47682b9-c270-40b1-b16d-90b627a5ce1b";
    private const string ControllerTrackingBlockId = "5817f7c0-f2a5-45f9-a5ca-64264e0166e8";
    private const string ControllerButtonsMapperBlockId = "80aa2eb2-af2f-40a9-ad9e-18044ce65bee";

    [MenuItem("IVRAR/Configure Official Meta XR Building Blocks", false, 2)]
    public static void ConfigureBuildingBlocks()
    {
        Debug.Log("[BuildingBlockConfigurator] Starting Meta XR Building Blocks configuration...");

        // 1. Meta_OVRCameraRig
        GameObject cameraRigGo = GameObject.Find("Meta_OVRCameraRig");
        if (cameraRigGo != null)
        {
            var bb = cameraRigGo.GetComponent<BuildingBlock>();
            if (bb == null)
            {
                bb = Undo.AddComponent<BuildingBlock>(cameraRigGo);
            }
            SetBuildingBlockData(bb, CameraRigBlockId, 1);

            var manager = cameraRigGo.GetComponent<OVRManager>();
            if (manager != null)
            {
                Undo.RecordObject(manager, "Set Tracking Origin FloorLevel");
                manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
                manager.usePositionTracking = true;
                manager.useRotationTracking = true;
            }
            Debug.Log("[BuildingBlockConfigurator] Configured CameraRig Building Block (ID: " + CameraRigBlockId + ")");
        }
        else
        {
            Debug.LogWarning("[BuildingBlockConfigurator] Meta_OVRCameraRig GameObject not found in active scene.");
        }

        // 2. Left Controller
        GameObject leftController = GameObject.Find("OVRControllerPrefab_Left");
        if (leftController != null)
        {
            var bb = leftController.GetComponent<BuildingBlock>();
            if (bb == null)
            {
                bb = Undo.AddComponent<BuildingBlock>(leftController);
            }
            SetBuildingBlockData(bb, ControllerTrackingBlockId, 1);
            Debug.Log("[BuildingBlockConfigurator] Configured Left Controller Tracking Building Block.");
        }

        // 3. Right Controller
        GameObject rightController = GameObject.Find("OVRControllerPrefab_Right");
        if (rightController != null)
        {
            var bb = rightController.GetComponent<BuildingBlock>();
            if (bb == null)
            {
                bb = Undo.AddComponent<BuildingBlock>(rightController);
            }
            SetBuildingBlockData(bb, ControllerTrackingBlockId, 1);
            Debug.Log("[BuildingBlockConfigurator] Configured Right Controller Tracking Building Block.");
        }

        // 4. Controller Buttons Mapper Building Block
        GameObject mapperGo = GameObject.Find("ControllerButtonsMapper");
        if (mapperGo == null)
        {
            mapperGo = new GameObject("ControllerButtonsMapper");
            Undo.RegisterCreatedObjectUndo(mapperGo, "Create ControllerButtonsMapper");
        }

        var mapperBb = mapperGo.GetComponent<BuildingBlock>();
        if (mapperBb == null) mapperBb = Undo.AddComponent<BuildingBlock>(mapperGo);
        SetBuildingBlockData(mapperBb, ControllerButtonsMapperBlockId, 1);

        var mapperComp = mapperGo.GetComponent<ControllerButtonsMapper>();
        if (mapperComp == null) mapperComp = Undo.AddComponent<ControllerButtonsMapper>(mapperGo);

        // 5. Interactive Media Selector Console
        GameObject selectorGo = GameObject.Find("Media_Selector_Console");
        if (selectorGo == null)
        {
            selectorGo = new GameObject("Media_Selector_Console");
            Undo.RegisterCreatedObjectUndo(selectorGo, "Create Media_Selector_Console");
        }
        var selector = selectorGo.GetComponent<HomeTheaterMediaSelector>();
        if (selector == null) selector = Undo.AddComponent<HomeTheaterMediaSelector>(selectorGo);

        var moviePlayer = UnityEngine.Object.FindAnyObjectByType<MovieScreenPlayer>();
        if (moviePlayer != null) selector.moviePlayer = moviePlayer;

        // Configure Controller Buttons Actions
        ConfigureMapperActions(mapperComp, moviePlayer, selector);
        Debug.Log("[BuildingBlockConfigurator] Configured Controller Buttons Mapper Building Block (ID: " + ControllerButtonsMapperBlockId + ")");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[BuildingBlockConfigurator] Meta XR Building Blocks and Media Selector successfully configured and scene saved.");
    }

    private static void ConfigureMapperActions(ControllerButtonsMapper mapper, MovieScreenPlayer moviePlayer, HomeTheaterMediaSelector selector)
    {
        var actions = new List<ControllerButtonsMapper.ButtonClickAction>();

        // --- RIGHT CONTROLLER ---
        // 1. Next Video Clip (Button A)
        var nextAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Right Controller] Button A: Next Video Clip",
            Button = OVRInput.Button.One,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (moviePlayer != null)
        {
            UnityEventTools.AddPersistentListener(nextAction.Callback, moviePlayer.PlayNextVideo);
        }
        actions.Add(nextAction);

        // 2. Play / Pause (Button B)
        var playPauseAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Right Controller] Button B: Play / Pause",
            Button = OVRInput.Button.Two,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (moviePlayer != null)
        {
            UnityEventTools.AddPersistentListener(playPauseAction.Callback, moviePlayer.TogglePlayPause);
        }
        actions.Add(playPauseAction);

        // 3. Volume Up (Right Thumbstick Up)
        var volUpAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Right Controller] Thumbstick Up: Volume +10%",
            Button = OVRInput.Button.SecondaryThumbstickUp,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (selector != null)
        {
            UnityEventTools.AddPersistentListener(volUpAction.Callback, selector.VolumeUp);
        }
        actions.Add(volUpAction);

        // 4. Volume Down (Right Thumbstick Down)
        var volDownAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Right Controller] Thumbstick Down: Volume -10%",
            Button = OVRInput.Button.SecondaryThumbstickDown,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (selector != null)
        {
            UnityEventTools.AddPersistentListener(volDownAction.Callback, selector.VolumeDown);
        }
        actions.Add(volDownAction);

        // 5. Restart Movie (Right Index Trigger)
        var restartAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Right Controller] Index Trigger: Restart Movie",
            Button = OVRInput.Button.SecondaryIndexTrigger,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (moviePlayer != null)
        {
            UnityEventTools.AddPersistentListener(restartAction.Callback, moviePlayer.RestartMovie);
        }
        actions.Add(restartAction);

        // --- LEFT CONTROLLER ---
        // 6. Previous Video Clip (Button X)
        var prevAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Left Controller] Button X: Previous Video Clip",
            Button = OVRInput.Button.Three,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (moviePlayer != null)
        {
            UnityEventTools.AddPersistentListener(prevAction.Callback, moviePlayer.PlayPreviousVideo);
        }
        actions.Add(prevAction);

        // 7. Toggle Media Selector Console (Button Y)
        var toggleMenuAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Left Controller] Button Y: Toggle Media Selector Menu",
            Button = OVRInput.Button.Four,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (selector != null)
        {
            UnityEventTools.AddPersistentListener(toggleMenuAction.Callback, selector.ToggleMenu);
        }
        actions.Add(toggleMenuAction);

        // 8. Rescan Folder (Left Menu Button)
        var refreshAction = new ControllerButtonsMapper.ButtonClickAction
        {
            Title = "[Left Controller] Menu Button: Rescan Video Folder",
            Button = OVRInput.Button.Start,
            ButtonMode = ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            Callback = new UnityEvent()
        };
        if (selector != null)
        {
            UnityEventTools.AddPersistentListener(refreshAction.Callback, selector.RefreshPlaylist);
        }
        actions.Add(refreshAction);

        mapper.ButtonClickActions = actions;
        EditorUtility.SetDirty(mapper);
    }

    private static void SetBuildingBlockData(BuildingBlock bb, string id, int ver)
    {
        SerializedObject so = new SerializedObject(bb);
        SerializedProperty idProp = so.FindProperty("blockId");
        if (idProp != null) idProp.stringValue = id;

        SerializedProperty verProp = so.FindProperty("version");
        if (verProp != null) verProp.intValue = ver;

        so.ApplyModifiedProperties();
    }
}
#endif
