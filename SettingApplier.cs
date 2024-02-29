using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace UnityBuilderAction
{
	public static class SettingApplier
	{
		private const string LocalKeystoreName = "localuser.keystore";
		private const string LocalKeystoreAlias = "localuser";
		private const string LocalKeystorePassword = "LocalKeystorePassword";

        public static BuildPlayerOptions ApplyLocalDevelopmentSettings(BuildTarget buildTarget, string outputPath, bool development)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
	                PlayerSettings.Android.keystoreName = LocalKeystoreName;
	                PlayerSettings.Android.keystorePass = LocalKeystorePassword;
	                PlayerSettings.Android.keyaliasName = LocalKeystoreAlias;
	                PlayerSettings.Android.keyaliasPass = LocalKeystorePassword;
                    break;
                }
                case BuildTarget.StandaloneOSX:
	                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                    break;
            }

			BuildPlayerOptions buildPlayerOptions = new()
			{
				scenes = GetScenesPaths(),
				locationPathName = outputPath,
				target = buildTarget,
				targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
				options = development ? BuildOptions.Development : BuildOptions.None
			};

			return buildPlayerOptions;
        }
        
		public static BuildPlayerOptions ApplyCommandLineSettings()
        {
            // Gather values from args
            Dictionary<string, string> options = CommandLineParser.GetValidatedOptions();

            // Set version for this build
            PlayerSettings.bundleVersion = options["buildVersion"];
            PlayerSettings.macOS.buildNumber = options["buildVersion"];
            PlayerSettings.Android.bundleVersionCode = int.Parse(options["androidVersionCode"]);

            // Apply build target
            BuildTarget buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
                    if (options.TryGetValue("androidKeystoreName", out string keystoreName) && !string.IsNullOrEmpty(keystoreName))
                    {
                      PlayerSettings.Android.useCustomKeystore = true;
                      PlayerSettings.Android.keystoreName = keystoreName;
                    }

                    if (options.TryGetValue("androidKeystorePass", out string keystorePass) && !string.IsNullOrEmpty(keystorePass))
                    {
                        PlayerSettings.Android.keystorePass = keystorePass;
                    }
                        
                    if (options.TryGetValue("androidKeyaliasName", out string keyaliasName) && !string.IsNullOrEmpty(keyaliasName))
                    {
                        PlayerSettings.Android.keyaliasName = keyaliasName;
                    }
                    
                    if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass) && !string.IsNullOrEmpty(keyaliasPass))
                    {
                        PlayerSettings.Android.keyaliasPass = keyaliasPass;
                    }
                    
                    if (options.TryGetValue("androidTargetSdkVersion", out string androidTargetSdkVersion) && !string.IsNullOrEmpty(androidTargetSdkVersion))
                    {
                        AndroidSdkVersions targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                        try
                        {
                            targetSdkVersion = (AndroidSdkVersions) Enum.Parse(typeof(AndroidSdkVersions), androidTargetSdkVersion);
                        }
                        catch
                        {
                            UnityEngine.Debug.Log("Failed to parse androidTargetSdkVersion! Fallback to AndroidApiLevelAuto");
                        }

                        PlayerSettings.Android.targetSdkVersion = targetSdkVersion;
                    }
                    
                    EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;

                    break;
                }
                case BuildTarget.StandaloneOSX:
	                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                    break;
            }

            // Determine sub target
            int buildSubTarget = 0;
            if (!options.TryGetValue("standaloneBuildSubtarget", out string subTargetValue) || !Enum.TryParse(subTargetValue, out StandaloneBuildSubtarget buildSubTargetValue)) 
            {
                buildSubTargetValue = default;
            }
            buildSubTarget = (int) buildSubTargetValue;
            
            // Build options
            BuildPlayerOptions buildPlayerOptions = new()
            {
                scenes = GetScenesPaths(),
                target = buildTarget,
                // targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
                locationPathName = options["customBuildPath"],
                // options = UnityEditor.BuildOptions.Development,
                subtarget = buildSubTarget
            };

            return buildPlayerOptions;
        }

		private static string[] GetScenesPaths()
		{
			string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
			return scenes;
		}
	}
}