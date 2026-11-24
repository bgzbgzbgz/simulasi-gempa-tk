using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

public static class SetupGirlCharacterEditor
{
    [MenuItem("Tools/Character/Setup Girl Character Complete")]
    public static void SetupComplete()
    {
        // 1. Configure Model Importer for girl_rigged_pbr.fbx
        string modelPath = "Assets/girl_rigged_pbr.fbx";
        var mi = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (mi != null)
        {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();
            Debug.Log("[SetupGirl] Model girl_rigged_pbr.fbx configured as Humanoid!");
        }

        // 2. Configure Animation Importers
        string[] animFiles = new string[]
        {
            "Assets/Animation/Idle_girls.fbx",
            "Assets/Animation/Walk_girl.fbx",
            "Assets/Animation/Running_girls.fbx",
            "Assets/Animation/Crouched Walking Girls.fbx"
        };

        foreach (var animPath in animFiles)
        {
            var ami = AssetImporter.GetAtPath(animPath) as ModelImporter;
            if (ami != null)
            {
                ami.animationType = ModelImporterAnimationType.Human;
                ami.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                ami.clipAnimations = new ModelImporterClipAnimation[0];
                ami.SaveAndReimport();

                var defaultClips = ami.defaultClipAnimations;
                if (defaultClips != null && defaultClips.Length > 0)
                {
                    for (int i = 0; i < defaultClips.Length; i++)
                    {
                        defaultClips[i].loopTime = true;
                    }
                    ami.clipAnimations = defaultClips;
                    ami.SaveAndReimport();
                }
                Debug.Log("[SetupGirl] Animation configured: " + animPath);
            }
        }

        // 3. Create or update Material
        string matPath = "Assets/Materials/Mat_Girl.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/tex_girl_tripo_4k.png");
        if (tex != null && mat != null)
        {
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
        }

        // 4. Create or update girlAnimator.controller
        string ctrlPath = "Assets/Animation/girlAnimator.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        }

        // Setup parameters
        bool hasSpeed = false;
        bool hasCrouch = false;
        foreach (var p in ctrl.parameters)
        {
            if (p.name == "Speed") hasSpeed = true;
            if (p.name == "isCrouching") hasCrouch = true;
        }
        if (!hasSpeed) ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        if (!hasCrouch) ctrl.AddParameter("isCrouching", AnimatorControllerParameterType.Bool);

        // Load animation clips from the imported FBXs
        AnimationClip idleClip = LoadAnimationClip("Assets/Animation/Idle_girls.fbx");
        AnimationClip walkClip = LoadAnimationClip("Assets/Animation/Walk_girl.fbx");
        AnimationClip runClip = LoadAnimationClip("Assets/Animation/Running_girls.fbx") ?? LoadAnimationClip("Assets/Animation/Running.fbx");
        AnimationClip crouchClip = LoadAnimationClip("Assets/Animation/Crouched Walking Girls.fbx");

        var rootStateMachine = ctrl.layers[0].stateMachine;
        rootStateMachine.states = new ChildAnimatorState[0]; // Reset

        // Create BlendTree for Locomotion (Speed)
        BlendTree blendTree;
        var blendTreeState = ctrl.CreateBlendTreeInController("Blend Tree", out blendTree);
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Speed";
        if (idleClip != null) blendTree.AddChild(idleClip, 0.0f);
        if (walkClip != null) blendTree.AddChild(walkClip, 0.5f);
        if (runClip != null) blendTree.AddChild(runClip, 1.0f);

        rootStateMachine.defaultState = blendTreeState;

        // Create Crouching State
        var crouchState = rootStateMachine.AddState("Crouching");
        if (crouchClip != null) crouchState.motion = crouchClip;

        // Transitions
        var toCrouch = blendTreeState.AddTransition(crouchState);
        toCrouch.AddCondition(AnimatorConditionMode.If, 0, "isCrouching");
        toCrouch.hasExitTime = false;
        toCrouch.duration = 0.15f;

        var fromCrouch = crouchState.AddTransition(blendTreeState);
        fromCrouch.AddCondition(AnimatorConditionMode.IfNot, 0, "isCrouching");
        fromCrouch.hasExitTime = false;
        fromCrouch.duration = 0.15f;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        Debug.Log("[SetupGirl] Successfully configured Rig, Material, and Animator Controller!");
    }

    private static AnimationClip LoadAnimationClip(string fbxPath)
    {
        var objs = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var o in objs)
        {
            if (o is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }

    [MenuItem("Tools/Character/Assemble Girl in Current Scene")]
    public static void AssembleGirlInScene()
    {
        var existingGirl = GameObject.Find("girl");
        if (existingGirl != null)
        {
            Object.DestroyImmediate(existingGirl);
        }

        var boy = GameObject.Find("boy");
        Vector3 spawnPos = boy != null ? boy.transform.position : new Vector3(0f, 0.14f, 0.5f);
        Quaternion spawnRot = boy != null ? boy.transform.rotation : Quaternion.identity;

        var girlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/girl_rigged_pbr.fbx");
        if (girlPrefab == null)
        {
            Debug.LogError("[SetupGirl] Assets/girl_rigged_pbr.fbx not found!");
            return;
        }

        GameObject girl = Object.Instantiate(girlPrefab, spawnPos, spawnRot);
        girl.name = "girl";

        // CharacterController
        var cc = girl.GetComponent<CharacterController>() ?? girl.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.52f, 0f);
        cc.height = 0.99f;
        cc.radius = 0.25f;
        cc.stepOffset = 0.3f;

        // BoyController (General character movement controller)
        var bc = girl.GetComponent<BoyController>() ?? girl.AddComponent<BoyController>();

        // Animator
        var anim = girl.GetComponent<Animator>() ?? girl.AddComponent<Animator>();
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/girlAnimator.controller");
        anim.runtimeAnimatorController = ctrl;

        Avatar girlAvatar = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/girl_rigged_pbr.fbx"))
        {
            if (obj is Avatar av) { girlAvatar = av; break; }
        }
        anim.avatar = girlAvatar;

        // Material on SkinnedMeshRenderer (Slot 0: Mat_Girl, Slot 1: Mat_Boy for pants)
        var smr = girl.GetComponentInChildren<SkinnedMeshRenderer>();
        var matGirl = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Girl.mat");
        var matBoy = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Boy.mat");
        if (smr != null)
        {
            if (matGirl != null && matBoy != null)
                smr.sharedMaterials = new Material[] { matGirl, matBoy };
            else if (matGirl != null)
                smr.sharedMaterial = matGirl;
        }

        // Duplicate NamePlate_Billboard from boy if present
        if (boy != null)
        {
            var boyBillboard = boy.transform.Find("NamePlate_Billboard");
            if (boyBillboard != null)
            {
                var girlBillboard = Object.Instantiate(boyBillboard.gameObject, girl.transform);
                girlBillboard.name = "NamePlate_Billboard";
                var bbComp = girlBillboard.GetComponent<PlayerNameBillboard>();
                if (bbComp != null)
                {
                    bbComp.targetTransform = null; // auto-detect girl head
                }
            }
        }

        // Add or ensure PlayerGenderManager on GameManager
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            var pgm = gm.GetComponent<PlayerGenderManager>() ?? gm.gameObject.AddComponent<PlayerGenderManager>();
            pgm.boyCharacter = boy;
            pgm.girlCharacter = girl;
            EditorUtility.SetDirty(gm);
        }

        // Initially disable girl in scene editor so boy remains default until Play mode
        girl.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupGirl] Successfully assembled 'girl' character in scene!");
    }
}
