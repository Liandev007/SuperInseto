using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SuperInseto.Editor
{
    // Generates native Unity assets only after Unity has imported and validated the REAL source files.
    // No guessed Avatar/clip fileIDs, no edits to Player/M1-M14 scene objects, no external animation downloads.
    [InitializeOnLoad]
    public static class PlayerAnimationPreviewBuilder
    {
        public const string Root = "Assets/SuperInseto/Characters/Player";
        public const string DefinitionPath = Root + "/Resources/SuperInseto/PlayerAnimationPreview.asset";
        public const string ControllerPath = Root + "/Animator/SuperInseto_Player.controller";
        public const string PrefabPath = Root + "/Prefabs/SuperInsetoVisual.prefab";
        public const string MaterialPath = Root + "/Materials/SuperInseto_Player.mat";
        const string ModelPath = Root + "/Model/T-Pose.fbx";
        const string TexturePath = Root + "/Model/Texture.png";
        const string Revision = "M14.5-preview-1";
        static bool scheduled, building;

        static PlayerAnimationPreviewBuilder() { Schedule(); }
        internal static void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += AutoBuild;
        }
        static void AutoBuild()
        {
            scheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || building) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            if (!File.Exists(ModelPath)) return;
            string fingerprint = Fingerprint();
            var existing = AssetDatabase.LoadAssetAtPath<PlayerAnimationDefinition>(DefinitionPath);
            if (existing && existing.IsUsable && existing.sourceFingerprint == fingerprint) return;
            string key = Revision + "/attempt/" + fingerprint;
            if (SessionState.GetBool(key, false)) return;
            SessionState.SetBool(key, true); // An invalid Avatar produces one actionable warning, never an import loop.
            Build(false);
        }

        [MenuItem("Super Inseto/M14.5/Build or Rebuild Player Preview")]
        public static void Rebuild() { Build(true); }

        [MenuItem("Super Inseto/M14.5/Select Animation Settings")]
        public static void SelectSettings()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<PlayerAnimationDefinition>(DefinitionPath);
            if (!Selection.activeObject) Debug.LogWarning("Build the M14.5 preview first.");
        }

        public static bool Build(bool explicitRequest)
        {
            if (building || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            building = true;
            try
            {
                RequireSourceFiles();
                EnsureFolder(Root + "/Materials"); EnsureFolder(Root + "/Animator");
                EnsureFolder(Root + "/Prefabs"); EnsureFolder(Root + "/Resources/SuperInseto");

                var modelImporter = GetImporter(ModelPath);
                ConfigureCommon(modelImporter);
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelImporter.sourceAvatar = null;
                modelImporter.importAnimation = false;
                modelImporter.SaveAndReimport();
                var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
                if (!avatar || !avatar.isValid || !avatar.isHuman)
                    throw new InvalidOperationException("T-Pose.fbx did not produce a valid Humanoid Avatar. Open its Rig/Configure tab; no invalid rig was forced and the placeholder remains available.");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (!model || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
                    throw new InvalidOperationException("T-Pose.fbx has no imported SkinnedMeshRenderer.");
                var sourceHierarchy = BoneHierarchy(model);
                var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
                foreach (var spec in PlayerClipCatalog.All)
                {
                    string path = Root + "/Animations/" + spec.file;
                    var importer = GetImporter(path);
                    ConfigureCommon(importer);
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;
                    importer.importAnimation = true;
                    var defaults = importer.defaultClipAnimations;
                    if (defaults.Length != 1)
                        throw new InvalidOperationException(spec.file + ": expected the inspected single animation take; found " + defaults.Length + ".");
                    var clip = defaults[0];
                    if (spec.first < clip.firstFrame - 0.1f || spec.last > clip.lastFrame + 0.1f)
                        throw new InvalidOperationException(spec.file + ": imported frame range differs from the inspected source. Review before cropping.");
                    clip.name = spec.name; clip.firstFrame = spec.first; clip.lastFrame = spec.last;
                    clip.loopTime = spec.loop; clip.loopPose = spec.loop;
                    clip.mirror = false; // Mirroring is per Animator state, including the alternating Heavy.
                    clip.lockRootRotation = true;
                    clip.keepOriginalOrientation = true;
                    clip.lockRootPositionXZ = false; // Extract displacement, then discard it with applyRootMotion=false.
                    clip.keepOriginalPositionXZ = true;
                    clip.lockRootHeightY = !spec.extractY;
                    clip.keepOriginalPositionY = !spec.extractY;
                    clip.heightFromFeet = spec.extractY;
                    clip.events = Array.Empty<AnimationEvent>(); // Equivalent phase bridge; never duplicate M3/M8/M9 callbacks.
                    importer.clipAnimations = new[] { clip };
                    importer.SaveAndReimport();
                    var importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!SameHierarchy(sourceHierarchy, BoneHierarchy(importedModel)))
                        throw new InvalidOperationException(spec.file + ": skeleton hierarchy differs from T-Pose; retargeting was not accepted.");
                    var motion = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                        .FirstOrDefault(c => c.name == spec.name && !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                    if (!motion || !motion.isHumanMotion || motion.length <= 0f)
                        throw new InvalidOperationException(spec.file + ": Unity did not import a usable Humanoid clip with the T-Pose Avatar.");
                    clips.Add(spec.name, motion);
                }

                var textureImporter = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                if (textureImporter == null) throw new InvalidOperationException("Texture.png importer unavailable.");
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.sRGBTexture = true;
                textureImporter.alphaSource = TextureImporterAlphaSource.None;
                textureImporter.mipmapEnabled = true;
                textureImporter.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) throw new InvalidOperationException("Existing URP Lit shader unavailable. Global pipeline settings were not changed.");
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (!material)
                {
                    material = new Material(shader) { name = "SuperInseto_Player" };
                    material.SetColor("_BaseColor", Color.white);
                    material.SetFloat("_Metallic", 0.15f); material.SetFloat("_Smoothness", 0.3f);
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                material.shader = shader; material.SetTexture("_BaseMap", texture); EditorUtility.SetDirty(material);

                var definition = AssetDatabase.LoadAssetAtPath<PlayerAnimationDefinition>(DefinitionPath);
                if (!definition)
                {
                    definition = ScriptableObject.CreateInstance<PlayerAnimationDefinition>();
                    AssetDatabase.CreateAsset(definition, DefinitionPath);
                }
                var controller = BuildController(clips, definition);
                var prefab = BuildPrefab(model, avatar, controller, material, definition, out var fittedScale, out var fittedOffset);
                ValidatePrefab(prefab, controller);
                definition.visualPrefab = prefab; definition.controller = controller;
                definition.sourceFingerprint = Fingerprint();
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
                WriteValidationReport(avatar, clips, fittedScale, fittedOffset);
                Debug.Log("M14.5: valid T-Pose Humanoid and 17 clips imported. Preview is ready for LOCAL visual/gameplay validation. Open Nave_TestGym and Play; main/source FBXs were not modified.");
                if (explicitRequest) Selection.activeObject = definition;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("M14.5 preview not ready: " + error.Message + "\nUse Super Inseto/M14.5/Build or Rebuild Player Preview after resolving the reported import issue.");
                return false;
            }
            finally { building = false; }
        }

        static void ConfigureCommon(ModelImporter importer)
        {
            importer.globalScale = 1f; importer.useFileScale = true;
            importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.optimizeGameObjects = false; // Hand bones must remain accessible to the M9 cast origin.
            importer.preserveHierarchy = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
        }
        static ModelImporter GetImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetImporter.GetAtPath(path) as ModelImporter
                ?? throw new InvalidOperationException("No ModelImporter for " + path);
        }
        static void RequireSourceFiles()
        {
            var paths = new List<string> { ModelPath, TexturePath };
            paths.AddRange(PlayerClipCatalog.All.Select(c => Root + "/Animations/" + c.file));
            foreach (var path in paths) if (!File.Exists(path)) throw new FileNotFoundException("Missing supplied asset: " + path);
        }
        static Dictionary<string, string> BoneHierarchy(GameObject model)
        {
            if (!model) throw new InvalidOperationException("Imported model hierarchy unavailable.");
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                if (!bone.name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase)) continue;
                string parent = bone.parent && bone.parent.name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase)
                    ? bone.parent.name : "";
                if (result.ContainsKey(bone.name)) throw new InvalidOperationException("Duplicate bone name: " + bone.name);
                result.Add(bone.name, parent);
            }
            if (result.Count != 65) throw new InvalidOperationException("Expected the inspected 65-bone Mixamo skeleton; found " + result.Count + ".");
            return result;
        }
        static bool SameHierarchy(Dictionary<string, string> a, Dictionary<string, string> b) => a.Count == b.Count
            && a.All(pair => b.TryGetValue(pair.Key, out var parent) && parent == pair.Value);

        static AnimatorController BuildController(Dictionary<string, AnimationClip> clips, PlayerAnimationDefinition settings)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            // This exact controller is generated content. Keep its asset/GUID stable across an explicit rebuild.
            controller.layers = Array.Empty<AnimatorControllerLayer>();
            foreach (var child in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
                if (child && child != controller) Object.DestroyImmediate(child, true);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var machine = new AnimatorStateMachine { name = "Base Layer" };
            AssetDatabase.AddObjectToAsset(machine, controller);
            controller.layers = new[] { new AnimatorControllerLayer { name = "Base Layer", defaultWeight = 1f, stateMachine = machine } };
            foreach (string p in new[] { "Speed", "LocomotionRate", "ClimbRate", "VerticalVelocity" })
                controller.AddParameter(p, AnimatorControllerParameterType.Float);
            foreach (string p in new[] { "IsGrounded", "IsSprinting", "IsClimbing", "IsMantling", "IsDodging", "IsDead" })
                controller.AddParameter(p, AnimatorControllerParameterType.Bool);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);

            var states = new AnimatorState[PlayerAnimationState.Names.Length];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = machine.AddState(PlayerAnimationState.Names[i], new Vector3(260f * (i % 4), 100f * (i / 4), 0f));
                states[i].writeDefaultValues = true;
                states[i].iKOnFeet = false;
                if (PlayerAnimationState.HasMotionTime((PlayerVisualState)i))
                {
                    controller.AddParameter(PlayerAnimationState.TimeNames[i], AnimatorControllerParameterType.Float);
                    states[i].timeParameter = PlayerAnimationState.TimeNames[i];
                    states[i].timeParameterActive = true;
                }
            }
            machine.defaultState = states[0];
            var tree = new BlendTree { name = "Idle Walk Run (actual m/s)", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(clips["Idle"], 0f);
            tree.AddChild(clips["Walk"], Mathf.Max(0.1f, settings.walkThreshold));
            tree.AddChild(clips["Run"], Mathf.Max(settings.walkThreshold + 0.1f, settings.runThreshold));
            states[0].motion = tree; states[0].speedParameter = "LocomotionRate"; states[0].speedParameterActive = true;
            string[] motions = { "", "JumpTakeoff", "Floating", "FallLanding", "Climb", "Mantle", "LightAttack01",
                "LightAttack02", "LightAttack03", "HeavyAttack", "HeavyAttack", "Dodge", "HitReaction", "Death", "ChitinImpact", "BioelectricStinger" };
            for (int i = 1; i < states.Length; i++) states[i].motion = clips[motions[i]];
            states[(int)PlayerVisualState.Light2].mirror = true;
            states[(int)PlayerVisualState.HeavyLeft].mirror = true;
            states[(int)PlayerVisualState.Climb].speedParameter = "ClimbRate";
            states[(int)PlayerVisualState.Climb].speedParameterActive = true;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static GameObject BuildPrefab(GameObject source, Avatar avatar, AnimatorController controller, Material material,
            PlayerAnimationDefinition settings, out float fittedScale, out Vector3 fittedOffset)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            try
            {
                root = new GameObject("PlayerVisualRoot"); SceneManager.MoveGameObjectToScene(root, scene);
                var fit = new GameObject("ModelFit").transform; fit.SetParent(root.transform, false);
                fit.localRotation = Quaternion.Euler(settings.modelEulerOffset);
                var model = Object.Instantiate(source, fit, false); model.name = "SuperInsetoVisual";
                foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 2;
                foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                var animator = model.GetComponent<Animator>();
                if (!animator) animator = model.AddComponent<Animator>();
                animator.avatar = avatar; animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();

                Bounds bounds = SkinnedBounds(root.transform);
                if (bounds.size.y < 0.01f || float.IsNaN(bounds.size.y) || float.IsInfinity(bounds.size.y))
                    throw new InvalidOperationException("Cannot fit the imported T-Pose mesh: invalid skinned bounds.");
                fittedScale = Mathf.Max(0.5f, settings.modelHeight) / bounds.size.y;
                fittedOffset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * fittedScale;
                fit.localScale = Vector3.one * fittedScale; fit.localPosition = fittedOffset;
                // Preserve the controller root at unit scale; this fit belongs exclusively to the visual prefab.
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (!prefab) throw new InvalidOperationException("Unity could not save the visual prefab.");
                return prefab;
            }
            finally
            {
                if (root) Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        static Bounds SkinnedBounds(Transform root)
        {
            bool any = false; Bounds bounds = default;
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = new Mesh();
                try
                {
                    renderer.BakeMesh(mesh, false);
                    foreach (var vertex in mesh.vertices)
                    {
                        Vector3 point = root.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                        if (!any) { bounds = new Bounds(point, Vector3.zero); any = true; }
                        else bounds.Encapsulate(point);
                    }
                }
                finally { Object.DestroyImmediate(mesh); }
            }
            if (!any) throw new InvalidOperationException("No skinned vertices were produced by Unity.");
            return bounds;
        }
        static void ValidatePrefab(GameObject prefab, AnimatorController controller)
        {
            var animators = prefab.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1 || animators[0].applyRootMotion || !animators[0].avatar.isValid
                || !animators[0].avatar.isHuman || animators[0].runtimeAnimatorController != controller)
                throw new InvalidOperationException("Visual prefab failed its single-Humanoid/no-root-motion contract.");
            if (prefab.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Visual prefab unexpectedly contains colliders.");
        }
        static string Fingerprint()
        {
            using (var hash = SHA256.Create())
            using (var buffer = new MemoryStream())
            {
                byte[] version = Encoding.UTF8.GetBytes(Revision); buffer.Write(version, 0, version.Length);
                var paths = new List<string> { ModelPath, TexturePath };
                paths.AddRange(PlayerClipCatalog.All.Select(c => Root + "/Animations/" + c.file));
                foreach (string path in paths)
                {
                    if (!File.Exists(path)) continue;
                    byte[] digest = hash.ComputeHash(File.ReadAllBytes(path)); buffer.Write(digest, 0, digest.Length);
                }
                return BitConverter.ToString(hash.ComputeHash(buffer.ToArray())).Replace("-", "");
            }
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        [Serializable] sealed class ValidationReport
        {
            public string source, avatarName, unityVersion, status;
            public bool avatarValid, humanoid, applyRootMotion;
            public float modelScale;
            public Vector3 modelOffset;
            public string[] clips;
        }
        static void WriteValidationReport(Avatar avatar, Dictionary<string, AnimationClip> clips, float scale, Vector3 offset)
        {
            var report = new ValidationReport { source = ModelPath, avatarName = avatar.name, unityVersion = Application.unityVersion,
                status = "Unity import verified; visual quality and M1-M14 gameplay regression still require local PlayMode validation.",
                avatarValid = avatar.isValid, humanoid = avatar.isHuman, applyRootMotion = false, modelScale = scale,
                modelOffset = offset, clips = clips.Select(c => c.Key + ": " + c.Value.length.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
                    + " s; loop=" + c.Value.isLooping + "; human=" + c.Value.isHumanMotion).ToArray() };
            string path = Root + "/Animator/ImportValidation.json";
            File.WriteAllText(path, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
        }
    }

    internal sealed class PlayerAnimationSourceWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
                if (path.StartsWith(PlayerAnimationPreviewBuilder.Root + "/", StringComparison.Ordinal)
                    && (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || path.EndsWith("Texture.png", StringComparison.Ordinal)))
                { PlayerAnimationPreviewBuilder.Schedule(); return; }
        }
    }
}
