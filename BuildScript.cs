using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace UnityBuilderAction
{
    public static class BuildScript
    {
#if UNITY_ANDROID
        [MenuItem("Build/Build Local Android Debug")]
        public static void BuildLocalAndroidDebug()
        {
            BuildPlayerOptions buildOptions = SettingApplier.ApplyLocalDevelopmentSettings(BuildTarget.Android, "build/Android.apk", true);
            Build(buildOptions);
        }

        [MenuItem("Build/Build Local Android Production")]
        public static void BuildLocalAndroidProduction()
        {
            BuildPlayerOptions buildOptions = SettingApplier.ApplyLocalDevelopmentSettings(BuildTarget.Android, "build/Android.apk", false);
            Build(buildOptions);
        }
#endif
        
        public static void BuildWithCommandLine()
        {
            BuildPlayerOptions buildOptions = SettingApplier.ApplyCommandLineSettings();
            Build(buildOptions);
        }
        
        private static void Build(BuildPlayerOptions buildOptions)
        {
            BuildReport buildSummary = BuildPipeline.BuildPlayer(buildOptions);
            
            BuildReporter.ReportSummary(buildSummary);
            ExitWithResult(buildSummary.summary.result);
        }
        
        private static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }
	}
}