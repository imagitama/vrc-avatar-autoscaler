#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using VRC.SDKBase.Editor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Editor;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation;
using VRC.SDKBase.Validation.Performance.Stats;
using VRCStation = VRC.SDK3.Avatars.Components.VRCStation;
using VRC.SDK3.Validation;
using VRC.Core;
using VRCSDK2;

public class VRC_Avatar_AutoScaler : EditorWindow
{
    VRCAvatarDescriptor sourceVrcAvatarDescriptor;
    // bool enablePublishingAvatars = false;
    List<AutoScalerInput> autoScalerInputs = new List<AutoScalerInput>() {
      new AutoScalerInput() {
        scaleAmount = 1.1f
      },
      new AutoScalerInput() {
        scaleAmount = 2.5f
      }
    };

    [MenuItem("PeanutTools/VRC Avatar AutoScaler _%#T")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRC_Avatar_AutoScaler>();
        window.titleContent = new GUIContent("VRC Avatar AutoScaler");
        window.minSize = new Vector2(250, 50);
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Select your VRChat avatar that will be used as a base for your different sizes", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        sourceVrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", sourceVrcAvatarDescriptor, typeof(VRCAvatarDescriptor));
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    
        if (sourceVrcAvatarDescriptor != null) {
            if (GUILayout.Button("Detect Existing Sizes"))
            {
                DetectExistingSizes();
            }

            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Label("It searches for game objects with the same name but with the scale value at the end");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Enter each of the sizes (float):", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawScalesInputs();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // enablePublishingAvatars = GUILayout.Toggle(enablePublishingAvatars, "Publish avatars");

            // EditorGUILayout.Space();
            // EditorGUILayout.Space();

            if (GUILayout.Button("Create", GUILayout.Width(100), GUILayout.Height(50)))
            {
                CreateAvatars();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Update", GUILayout.Width(100), GUILayout.Height(50)))
            {
                UpdateAvatars();
            }

            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Label("Note that it does NOT re-scale each avatar (delete the objects and Create)");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        GUILayout.Label("Download new versions: https://github.com/imagitama/vrc-avatar-autoscaler");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("https://twitter.com/@HiPeanutBuddha");
        GUILayout.Label("Peanut#1756");
    }

    void DetectExistingSizes() {
        string sourceObjectName = sourceVrcAvatarDescriptor.gameObject.name;

        GameObject[] allRootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        autoScalerInputs = new List<AutoScalerInput>();

        foreach (GameObject rootGameObject in allRootGameObjects) {
            string rootGameObjectName = rootGameObject.name;

            if (rootGameObjectName.Contains(sourceObjectName)) {
                string[] chunks = rootGameObjectName.Split(new string[] { " - " }, System.StringSplitOptions.None);

                if (chunks.Length == 2) {
                    string scaleValue = chunks[1];
                    autoScalerInputs.Add(new AutoScalerInput() {
                        scaleAmount = float.Parse(scaleValue)
                    });
                }
            }
        }
    }

    void DrawScalesInputs() {
      autoScalerInputs.ForEach(autoScalerInput => {
          EditorGUILayout.BeginHorizontal();

          autoScalerInput.scaleAmount = EditorGUILayout.FloatField(autoScalerInput.scaleAmount);

          if (GUILayout.Button("Remove")) {
            RemoveAutoScalerInput(autoScalerInput);
          }

          EditorGUILayout.EndHorizontal();
      });

      if (GUILayout.Button("Add")) {
        AddAutoScalerInput();
      }
    }

    void RemoveAutoScalerInput(AutoScalerInput autoScalerInput) {
      autoScalerInputs.Remove(autoScalerInput);
    }
    
    void AddAutoScalerInput() {
      autoScalerInputs.Add(new AutoScalerInput());
    }

    void CreateAvatars() {
        Debug.Log("Found " + autoScalerInputs.Count + " scale inputs");

        CreateScaledAvatars();

        // if (enablePublishingAvatars) {
        //     PublishAvatars();
        // }

        Debug.Log("Done");
    }

    void UpdateAvatars() {
        Debug.Log("Found " + autoScalerInputs.Count + " scale inputs");

        List<ClonedAvatarData> existingAvatarClonedData = GetExistingClonedAvatarData();

        List<VRCAvatarDescriptor> existingClonedAvatars = GetExistingClonedAvatars();

        Debug.Log("Re-creating " + existingClonedAvatars.Count + " avatars...");

        foreach (VRCAvatarDescriptor item in existingClonedAvatars) {
            DestroyImmediate(item.gameObject);
        }

        CreateScaledAvatarsWithData(existingAvatarClonedData);

        // if (enablePublishingAvatars) {
        //     PublishAvatars();
        // }

        Debug.Log("Done");
    }

    void CreateScaledAvatarsWithData(List<ClonedAvatarData> avatarClonedDataItems) {
        foreach (ClonedAvatarData item in avatarClonedDataItems) {
            GameObject avatarGameObject = GetGameObjectFromVrcAvatarDescriptor(sourceVrcAvatarDescriptor);

            GameObject clonedGameObject = Instantiate(avatarGameObject, item.position, Quaternion.identity);

            clonedGameObject.name = item.gameObjectName;

            clonedGameObject.transform.localScale = item.scale;
            VRCAvatarDescriptor descriptor = clonedGameObject.GetComponent<VRCAvatarDescriptor>();
            descriptor.ViewPosition = item.viewPosition;

            PipelineManager pipelineManager = clonedGameObject.GetComponent<PipelineManager>();
            pipelineManager.blueprintId = item.blueprintId;
        }
    }

    List<VRCAvatarDescriptor> GetExistingClonedAvatars() {
        VRCAvatarDescriptor[] vrcAvatarDescriptors = (VRCAvatarDescriptor[]) GameObject.FindObjectsOfType (typeof(VRCAvatarDescriptor));

        return new List<VRCAvatarDescriptor>(Array.FindAll(vrcAvatarDescriptors, x => x.gameObject != sourceVrcAvatarDescriptor.gameObject));
    }

    List<ClonedAvatarData> GetExistingClonedAvatarData() {
        List<VRCAvatarDescriptor> existingAvatarDescriptors = GetExistingClonedAvatars();
        List<ClonedAvatarData> clonedAvatarDataItems = new List<ClonedAvatarData>();

        foreach (VRCAvatarDescriptor existingAvatarDescriptor in existingAvatarDescriptors) {
            PipelineManager pipelineManager = existingAvatarDescriptor.gameObject.GetComponent<PipelineManager>();
            
            clonedAvatarDataItems.Add(new ClonedAvatarData() {
                position = existingAvatarDescriptor.gameObject.transform.position,
                scale = existingAvatarDescriptor.gameObject.transform.localScale,
                blueprintId = pipelineManager.blueprintId,
                viewPosition = existingAvatarDescriptor.ViewPosition,
                gameObjectName = existingAvatarDescriptor.gameObject.name
            });
        }

        return clonedAvatarDataItems;
    }

    void PublishAvatars() {
        List<VRCAvatarDescriptor> existingClonedAvatars = GetExistingClonedAvatars();

        Debug.Log("Publishing " + existingClonedAvatars.Count + " avatars...");

        foreach (VRCAvatarDescriptor item in existingClonedAvatars) {
            PublishAvatar(item);
        }

        Debug.Log("Avatars have been published");
    }

    void PublishAvatar(VRCAvatarDescriptor avatar) {
        RuntimeBlueprintCreation blueprintCreator = avatar.gameObject.AddComponent(typeof(RuntimeBlueprintCreation)) as RuntimeBlueprintCreation;

        blueprintCreator.shouldUpdateImageToggle = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.contentSex = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.contentViolence = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.contentGore = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.contentOther = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.developerAvatar = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.sharePrivate = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.sharePublic = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        blueprintCreator.tagFallback = avatar.gameObject.AddComponent(typeof(UnityEngine.UI.Toggle)) as UnityEngine.UI.Toggle;
        
        blueprintCreator.pipelineManager = avatar.gameObject.GetComponent<PipelineManager>();

        blueprintCreator.SetupUpload();
    }

    void CreateScaledAvatars() {
        Debug.Log("Creating scaled avatar...");

        if (!sourceVrcAvatarDescriptor) {
            Debug.Log("Cannot continue without a source avatar");
            return;
        }

        autoScalerInputs.ForEach(autoScalerInput => {
            GameObject avatarGameObject = GetGameObjectFromVrcAvatarDescriptor(sourceVrcAvatarDescriptor);

            float newPositionX = GetXPositionFromInput(autoScalerInput);

            Vector3 newPosition = avatarGameObject.transform.position;
            newPosition.x = newPositionX;

            GameObject clonedGameObject = Instantiate(avatarGameObject, newPosition, Quaternion.identity);

            NameAvatarGameObject(clonedGameObject, autoScalerInput.scaleAmount);

            ScaleAvatar(clonedGameObject, autoScalerInput.scaleAmount);
        });

        Debug.Log("Created scaled avatars");
    }

    void NameAvatarGameObject(GameObject gameObject, float scaleAmount) {
        String newName = $" - {scaleAmount}";
        gameObject.name = gameObject.name.Replace("(Clone)", newName);
    }

    void ScaleAvatar(GameObject gameObject, float scaleAmount) {
        Vector3 currentScale = gameObject.transform.localScale;
        Vector3 newScale = currentScale * scaleAmount;

        gameObject.transform.localScale = newScale;

        VRCAvatarDescriptor avatar = gameObject.GetComponent<VRCAvatarDescriptor>();
        Vector3 newViewPosition = avatar.ViewPosition;
        newViewPosition.y = newViewPosition.y * scaleAmount; // height
        newViewPosition.z = newViewPosition.z * scaleAmount; // depth

        avatar.ViewPosition = newViewPosition;
    }

    float GetPreviousScaleAmount(AutoScalerInput autoScalerInput, List<AutoScalerInput> autoScalerInputs) {
        int currentIndex = autoScalerInputs.IndexOf(autoScalerInput);
        int previousIndex = currentIndex--;

        if (previousIndex >= 0) {
            return autoScalerInputs[previousIndex].scaleAmount;
        }

        return 1.0f;
    }

    GameObject GetGameObjectFromVrcAvatarDescriptor(VRCAvatarDescriptor vrcAvatarDescriptor) {
        return vrcAvatarDescriptor.gameObject;
    }
    
    float GetXPositionFromInput(AutoScalerInput autoScalerInput) {
        float sourceAvatarWidth = GetSourceAvatarWidth() + 1f;
        float totalAvatarsWidth = sourceAvatarWidth;

        foreach (AutoScalerInput item in autoScalerInputs) {
            if (item == autoScalerInput) {
                return totalAvatarsWidth;
            }

            totalAvatarsWidth = totalAvatarsWidth + (sourceAvatarWidth * item.scaleAmount);
        }

        return totalAvatarsWidth;
    }

    float GetSourceAvatarWidth() {
        GameObject sourceAvatarGameObject = GetGameObjectFromVrcAvatarDescriptor(sourceVrcAvatarDescriptor);
        return GetWidthOfGameObject(sourceAvatarGameObject);
    }

    SkinnedMeshRenderer GetChildMeshRenderer(GameObject gameObject) {
        foreach (Transform child in gameObject.transform) {
            SkinnedMeshRenderer childMeshRenderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();

            if (childMeshRenderer) {
                return childMeshRenderer;
            }
        }

        return null;
    }

    float GetWidthOfGameObject(GameObject gameObject) {
        SkinnedMeshRenderer meshRenderer = GetChildMeshRenderer(gameObject);
        return meshRenderer.bounds.size.x;
    }
}

#endif