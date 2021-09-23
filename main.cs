#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation;
using VRC.SDKBase.Validation.Performance.Stats;
using VRCStation = VRC.SDK3.Avatars.Components.VRCStation;
using VRC.SDK3.Validation;
using VRC.Core;
using VRCSDK2;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation;
using VRC.SDKBase.Validation.Performance.Stats;
using VRC.SDK3;
using VRC.SDK3.Validation;

public class VRC_Avatar_AutoScaler : EditorWindow
{
    VRCAvatarDescriptor sourceVrcAvatarDescriptor;
    VRCAvatarDescriptor lastSourceVrcAvatarDescriptor;
    bool enablePublishingAvatars = false;
    List<AutoScalerInput> autoScalerInputs = new List<AutoScalerInput>() {
      new AutoScalerInput() {
        scaleAmount = 1.1f
      },
      new AutoScalerInput() {
        scaleAmount = 2.5f
      }
    };
    bool hasInjectedIntoForm = false;
    ApiAvatar sourceApiAvatar;
    bool isPopulatingSourceApiAvatar = false;
    bool hasClickedOnUpload = false;
    bool hasSetupEditorReadyCheck = false;
    List<VRCAvatarDescriptor> publishQueue;
    bool isUploading = false;

    [MenuItem("PeanutTools/VRC Avatar AutoScaler _%#T")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRC_Avatar_AutoScaler>();
        window.titleContent = new GUIContent("VRC Avatar AutoScaler");
        window.minSize = new Vector2(250, 50);
    }

    // this happens every render of the editor (including play mode)
    void OnGUI()
    {
        if (isUploading) {
            GUILayout.Label("Uploading...");
            return;
        }

        if (publishQueue != null && publishQueue.Count > 0) {
            GUILayout.Label("There are " + publishQueue.Count + " avatars in the queue to publish", EditorStyles.boldLabel);
        }

        if (EditorApplication.isPlaying) {
            GUILayout.Label("Cannot do anything while in play mode");
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("Source Avatar", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("Select your VRChat avatar that will be used as a base for your different sizes", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        sourceVrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", sourceVrcAvatarDescriptor, typeof(VRCAvatarDescriptor));

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (lastSourceVrcAvatarDescriptor != sourceVrcAvatarDescriptor) {
            Debug.Log("Source avatar has changed");

            lastSourceVrcAvatarDescriptor = sourceVrcAvatarDescriptor;

            if (sourceVrcAvatarDescriptor != null && isPopulatingSourceApiAvatar == false) {
                PopulateSourceApiAvatar();
            }
        }

        if (isPopulatingSourceApiAvatar) {
            GUILayout.Label("Getting avatar details...");

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        if (sourceApiAvatar != null) {
            GUILayout.Label($"Name: {sourceApiAvatar.name}");

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        if (sourceVrcAvatarDescriptor != null) {
            if (GUILayout.Button("Refresh"))
            {
                PopulateSourceApiAvatar();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        DrawUILine();

        GUILayout.Label("Create", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Enter each of the sizes (float):", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        DrawScalesInputs();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Create"))
        {
            if (CanStart()) {
                CreateAvatars();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        DrawUILine();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Update", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        GUILayout.Label("By clicking this button you will update any VRC avatars in the scene (except the source):", EditorStyles.wordWrappedLabel);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Update"))
        {
            if (CanStart()) {
                UpdateAvatars();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        DrawUILine();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("Publish", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        enablePublishingAvatars = GUILayout.Toggle(enablePublishingAvatars, "Enable publishing avatars");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (enablePublishingAvatars) {
            GUILayout.Label("When you click the button below the plugin will automatically publish your avatar to VRChat.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("The name and description will be copied from your source avatar except the scale will be appended to the end.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (APIUser.CurrentUser == null) {
                GUILayout.Label("Login not detected - open VRC SDK panel and ensure you are logged in", EditorStyles.boldLabel);
            
            } else if (sourceApiAvatar == null) {
                GUILayout.Label("Source avatar details not detected - please click the Refresh button", EditorStyles.boldLabel);
            } else {
                if (GUILayout.Button("Publish"))
                {
                    if (CanStart()) {
                        PublishAvatars();
                    }
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        DrawUILine();

        GUILayout.Label("Download new versions: https://github.com/imagitama/vrc-avatar-autoscaler");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("https://twitter.com/@HiPeanutBuddha");
        GUILayout.Label("Peanut#1756");
    }

    void DrawUILine()
    {
        var rect = EditorGUILayout.BeginHorizontal();
        Handles.color = Color.gray;
        Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    void ProcessQueue() {
        if (publishQueue == null || publishQueue.Count == 0) {
            Debug.Log("No avatars need to be published");
            return;
        }

        Debug.Log("There are " + publishQueue.Count + " avatars waiting to be published");
        
        PublishFirstAvatarInQueue();
    }

    void SetupEditorReadyCheck() {
        if (!hasSetupEditorReadyCheck) {
            RuntimeBlueprintCreation blueprintCreation = GameObject.Find("VRCSDK").GetComponent<RuntimeBlueprintCreation>();
            blueprintCreation.blueprintPanel.SetActive(false);

            hasSetupEditorReadyCheck = true;
        }
    }
    
    bool IsEditorReadyForInjection() {
        // on first render of builder panel, SDK will fetch avatar to know how to render the panel
        // part of this render is enabling the panel
        // we disable the panel so that we can detect when the SDK has finished
        RuntimeBlueprintCreation blueprintCreation = GameObject.Find("VRCSDK").GetComponent<RuntimeBlueprintCreation>();
        return blueprintCreation.blueprintPanel.activeSelf;
    }

    VRCAvatarDescriptor GetAvatarFromBlueprintId(string blueprintId) {
        PipelineManager[] pipelineManagers = FindObjectsOfType<PipelineManager>();

        foreach (PipelineManager pipelineManager in pipelineManagers) {
            if (pipelineManager.blueprintId == blueprintId) {
                return pipelineManager.gameObject.GetComponent<VRCAvatarDescriptor>();
            }
        }

        return null;
    }

    float CalculateScaleAmountForAvatar(VRCAvatarDescriptor avatar) {
        float sourceScaleX = sourceVrcAvatarDescriptor.gameObject.transform.localScale.x;
        float currentScaleX = avatar.gameObject.transform.localScale.x;
        return currentScaleX / sourceScaleX;
    }

    // void RemoveFirstAvatarFromQueue() {
    //     gameObjectNamesToPublish = gameObjectNamesToPublish.Skip(1).ToArray();
    // }

    void PopulateSourceApiAvatar() {
        isPopulatingSourceApiAvatar = true;

        PipelineManager pipelineManager = sourceVrcAvatarDescriptor.gameObject.GetComponent<PipelineManager>();
        string sourceBlueprintId = pipelineManager.blueprintId;

        Debug.Log("Source avatar has blueprint ID: " + sourceBlueprintId);

        ApiAvatar.FetchList(
            delegate (IEnumerable<ApiAvatar> obj)
            {
                Debug.Log("Found " + obj.Count() + " published avatars");

                foreach (ApiAvatar apiAvatar in obj) {
                    if (apiAvatar.id == sourceBlueprintId) {
                        Debug.Log("Found the avatar: " + apiAvatar.name);
                        sourceApiAvatar = apiAvatar;
                    }
                }
                
                isPopulatingSourceApiAvatar = false;
            },
            delegate (string obj)
            {
                Debug.LogError("Error fetching your uploaded avatars:\n" + obj);
            },
            ApiAvatar.Owner.Mine,
            ApiAvatar.ReleaseStatus.All
        );
    }

    Boolean CanStart() {
        return sourceVrcAvatarDescriptor != null;
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
        // ApiAvatar apiAvatar = GetApiAvatarFromSource();

        List<VRCAvatarDescriptor> existingClonedAvatars = GetExistingClonedAvatars();

        Debug.Log("Publishing " + existingClonedAvatars.Count + " avatars...");

        publishQueue = existingClonedAvatars;

        PublishFirstAvatarInQueue();
    }

    void PublishFirstAvatarInQueue() {
        if (publishQueue.Count == 0) {
            Debug.Log("Cannot publish first avatar in queue as queue is empty");
            return;
        }

        Debug.Log("Publishing first avatar in queue...");

        VRCAvatarDescriptor avatar = publishQueue[0];
        
        PublishAvatar(avatar);
    }

    string PrepareVRCPathForS3(string abPath, string blueprintId, int version, AssetVersion assetVersion)
    {
        string uploadVrcPath = Application.temporaryCachePath + "/" + blueprintId + "_" + version.ToString() + "_" + Application.unityVersion + "_" + assetVersion.ApiVersion + "_" + VRC.Tools.Platform + "_" + API.GetServerEnvironmentForApiUrl() + System.IO.Path.GetExtension(abPath);
        uploadVrcPath.Trim();
        uploadVrcPath.Replace(' ', '_');

        if (System.IO.File.Exists(uploadVrcPath))
            System.IO.File.Delete(uploadVrcPath);

        System.IO.File.Copy(abPath, uploadVrcPath);

        return uploadVrcPath;
    }

    async void PublishAvatar(VRCAvatarDescriptor avatar) {
        if (APIUser.CurrentUser == null) {
            Debug.Log("Cannot continue without being logged in");
            return;
        }

        if (sourceApiAvatar == null) {
            Debug.Log("Cannot publish avatar without a source API avatar");
            return;
        }

        float scaleAmount = CalculateScaleAmountForAvatar(avatar);

        Debug.Log("Calculated scale amount " + scaleAmount);

        isUploading = true;

        BuildAvatar(avatar);

        return;

        // ApiAvatar apiAvatar = CreateAvatarFromSource(scaleAmount);

        // bool isUpdate = false;
        // string abPath = UnityEditor.EditorPrefs.GetString("currentBuildingAssetBundlePath");
        // string avatarId = apiAvatar.id;
        // int version = isUpdate ? apiAvatar.version + 1 : 1;
        // string uploadVrcPath = PrepareVRCPathForS3(abPath, avatarId, version, ApiAvatar.VERSION);

        // Debug.Log("VRC path built");

        // UploadAssetBundleAndSave(uploadVrcPath, apiAvatar);
    }

    void BuildAvatar(VRCAvatarDescriptor avatar) {
        Debug.Log("Building...");

        isUploading = false;
        
        VRC_SdkBuilder.ExportCurrentAvatarResource(avatar.gameObject, false);
        
        Debug.Log("Has been built");
    }

    static void ShowMethods(Type type)
    {
        foreach (var method in type.GetMethods())
        {
            var parameters = method.GetParameters();
            var parameterDescriptions = string.Join
                (", ", method.GetParameters()
                             .Select(x => x.ParameterType + " " + x.Name)
                             .ToArray());

            Console.WriteLine("{0} {1} ({2})",
                              method.ReturnType,
                              method.Name,
                              parameterDescriptions);
        }
    }

    async void UploadAssetBundleAndSave(string filepath, ApiAvatar apiAvatar) {
        Debug.Log("Uploading " + filepath + "...");

        // empty space for 2nd arg means force create new file
        ApiFileHelper.Instance.UploadFile(filepath, "", "foo", 
            delegate (ApiFile apiFile, string message) {
                Debug.Log("File uploaded successfully");

                SaveAvatar(apiAvatar);
            },
            delegate (ApiFile apiFile, string error) {
                Debug.Log("File failed to upload: " + error);
            },
                            delegate (ApiFile apiFile, string status, string subStatus, float pct)
                {
                    Debug.Log("Uploading...");
                },
                delegate (ApiFile apiFile) {
                Debug.Log("Cancelled!");
                return true;
            });
    }

    ApiAvatar CreateAvatarFromSource(float scaleAmount) {
        ApiAvatar apiAvatar = new ApiAvatar();
        apiAvatar.name = $"{sourceApiAvatar.name} - {scaleAmount}";
        apiAvatar.description = sourceApiAvatar.description;
        apiAvatar.releaseStatus = sourceApiAvatar.releaseStatus;
        apiAvatar.tags = sourceApiAvatar.tags;
        return apiAvatar;
    }

    void SaveAvatar(ApiAvatar apiAvatar) {    
        Debug.Log("Saving avatar \"" + apiAvatar.name + "\"...");

        apiAvatar.Save(
            (c) => {
                Debug.Log("Avatar \"" + apiAvatar.name + "\" has been saved!");
                isUploading = false;
            },
            (c) => {
                Debug.LogError(c.Error);
                isUploading = false;
            }
        );
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

            PipelineManager pipelineManager = clonedGameObject.GetComponent<PipelineManager>();
            pipelineManager.blueprintId = "";

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