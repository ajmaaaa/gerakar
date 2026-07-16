using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build;
using Vuforia;

namespace GerakAR.Editor
{
    public class BuildVuforiaPrototype
    {
        [MenuItem("Build/Build Vuforia Prototype")]
        public static void PerformBuild()
        {
            Debug.Log("[VuforiaPrototype] Starting build process...");

            // --- Save Scene State ---
            string prevScenePath = EditorSceneManager.GetActiveScene().path;

            // --- Save PlayerSettings State ---
            BuildTargetGroup originalTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
            
            bool originalUseDefaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            UnityEngine.Rendering.GraphicsDeviceType[] originalGraphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            
            AndroidArchitecture originalArch = PlayerSettings.Android.targetArchitectures;
            ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);

            string originalProductName = PlayerSettings.productName;
            string originalAppId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            bool originalDevBuild = EditorUserBuildSettings.development;
            AndroidSdkVersions originalMinSdkVersion = PlayerSettings.Android.minSdkVersion;

            try
            {
                // Load Scene (without modifying or saving)
                string scenePath = "Assets/Scenes/VuforiaPrototype.unity";
                if (!File.Exists(scenePath))
                {
                    Debug.LogError($"[VuforiaPrototype] Scene not found at {scenePath}");
                    return;
                }
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // --- 1. Preflight Read-Only Checks ---
                Debug.Log("[VuforiaPrototype] Running preflight read-only checks...");

                // Vuforia Configuration available
                var vuforiaConfig = VuforiaConfiguration.Instance;
                if (vuforiaConfig == null)
                {
                    Debug.LogError("[VuforiaPrototype] Preflight failed: VuforiaConfiguration.Instance is null.");
                    return;
                }
                Debug.Log("[VuforiaPrototype] Vuforia Engine available: OK");

                // License Key not empty (NEVER print its value)
                var vuforiaProp = vuforiaConfig.Vuforia;
                var ufoProp = vuforiaProp.GetType().GetProperty("UfoLicenseKey", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                string ufoLicenseKey = ufoProp != null ? ufoProp.GetValue(vuforiaProp) as string : null;

                if (string.IsNullOrEmpty(vuforiaConfig.Vuforia.LicenseKey) && string.IsNullOrEmpty(ufoLicenseKey))
                {
                    Debug.LogError("[VuforiaPrototype] Preflight failed: Both Vuforia License Key and Ufo License Key are null or empty.");
                    return;
                }
                Debug.Log("[VuforiaPrototype] Vuforia License Key configured (not empty): OK");

                // Track Device Pose Off
                if (vuforiaConfig.DeviceTracker.AutoInitAndStartTracker)
                {
                    Debug.LogError("[VuforiaPrototype] Preflight failed: Device Tracker is configured to start automatically (Track Device Pose must be Off).");
                    return;
                }
                Debug.Log("[VuforiaPrototype] Track Device Pose: Off (OK)");

                // ARCore Requirement: DON'T USE
                if (vuforiaConfig.DeviceTracker.ARCoreRequirementSetting != VuforiaConfiguration.DeviceTrackerConfiguration.ARCoreRequirement.DONT_USE)
                {
                    Debug.LogError($"[VuforiaPrototype] Preflight failed: ARCore Requirement is set to '{vuforiaConfig.DeviceTracker.ARCoreRequirementSetting}' (must be 'DONT_USE').");
                    return;
                }
                Debug.Log("[VuforiaPrototype] ARCore Requirement: DON'T USE (OK)");

                // Include ARCore: Off (AutoImportArcoreSetting)
                if (vuforiaConfig.DeviceTracker.AutoImportArcoreSetting)
                {
                    Debug.LogError("[VuforiaPrototype] Preflight failed: Include ARCore library is enabled (AutoImportArcoreSetting must be Off).");
                    return;
                }
                Debug.Log("[VuforiaPrototype] Include ARCore library: Off (OK)");

                // Camera Validation (exactly 1 active Camera with VuforiaBehaviour)
                var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                var activeCameras = cameras.Where(c => c.gameObject.activeInHierarchy).ToList();
                if (activeCameras.Count != 1)
                {
                    Debug.LogError($"[VuforiaPrototype] Preflight failed: Expected exactly 1 active Camera, but found {activeCameras.Count}.");
                    return;
                }
                var camera = activeCameras[0];

                var vuforiaBehaviour = camera.GetComponent<VuforiaBehaviour>();
                if (vuforiaBehaviour == null)
                {
                    Debug.LogError("[VuforiaPrototype] Preflight failed: Active camera does not have VuforiaBehaviour component.");
                    return;
                }
                Debug.Log("[VuforiaPrototype] Exactly one active camera with VuforiaBehaviour: OK");

                // Forbidden AR Foundation components check
                string[] forbiddenComponents = {
                    "ARSession",
                    "XROrigin",
                    "ARCameraManager",
                    "ARCameraBackground",
                    "ARTrackedImageManager"
                };
                var allGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var g in allGameObjects)
                {
                    foreach (var component in g.GetComponents<Component>())
                    {
                        if (component != null && forbiddenComponents.Contains(component.GetType().Name))
                        {
                            Debug.LogError($"[VuforiaPrototype] Preflight failed: Found forbidden AR Foundation component '{component.GetType().Name}' on GameObject '{g.name}'.");
                            return;
                        }
                    }
                }
                Debug.Log("[VuforiaPrototype] Forbidden AR Foundation components check: OK");

                // --- 2. Configure PlayerSettings for Build ---
                Debug.Log("[VuforiaPrototype] Configuring PlayerSettings for Android build...");
                
                // Switch build target to Android first
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                // Graphics API: OpenGLES3 only (disable Vulkan)
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

                // Architectures and backend: IL2CPP + ARM64
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

                // Suffix app identifier & Product name
                PlayerSettings.productName = "GerakAR Vuforia Prototype";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, originalAppId + ".vuforiaprototype");
                EditorUserBuildSettings.development = true;

                // --- 3. Run Build ---
                Directory.CreateDirectory("Builds");
                string apkPath = "Builds/GerakAR-VuforiaPrototype.apk";

                var buildOptions = new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.Development
                };

                Debug.Log($"[VuforiaPrototype] Initiating BuildPlayer for {apkPath}...");
                var report = BuildPipeline.BuildPlayer(buildOptions);
                var summary = report.summary;

                if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    Debug.Log($"[VuforiaPrototype] Build succeeded! File: {apkPath} ({summary.totalSize} bytes)");
                }
                else
                {
                    Debug.LogError($"[VuforiaPrototype] Build failed with result: {summary.result}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[VuforiaPrototype] Exception occurred during build: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                // --- 4. Restore PlayerSettings State ---
                Debug.Log("[VuforiaPrototype] Restoring initial PlayerSettings state...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(originalTargetGroup, originalTarget);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, originalUseDefaultGraphics);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, originalGraphicsAPIs);
                PlayerSettings.Android.targetArchitectures = originalArch;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, originalBackend);

                PlayerSettings.productName = originalProductName;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, originalAppId);
                EditorUserBuildSettings.development = originalDevBuild;
                PlayerSettings.Android.minSdkVersion = originalMinSdkVersion;

                // --- 5. Restore Scene State ---
                if (!string.IsNullOrEmpty(prevScenePath) && File.Exists(prevScenePath))
                {
                    Debug.Log($"[VuforiaPrototype] Restoring previously active scene: {prevScenePath}");
                    EditorSceneManager.OpenScene(prevScenePath, OpenSceneMode.Single);
                }
            }
        }

        [MenuItem("Tools/Reflect Vuforia ARCore Type")]
        public static void ReflectARCoreType()
        {
            var config = VuforiaConfiguration.Instance;
            if (config != null)
            {
                var type = config.DeviceTracker.ARCoreRequirementSetting.GetType();
                Debug.Log($"=== ARCoreRequirementSetting Type: {type.FullName} ===");
                foreach (var name in Enum.GetNames(type))
                {
                    Debug.Log($"Enum value: {name} (Value: {(int)Enum.Parse(type, name)})");
                }
            }
            else
            {
                Debug.LogError("VuforiaConfiguration.Instance is null!");
            }
        }
    }
}
