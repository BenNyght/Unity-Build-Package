using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenNyght.Utility.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BenNyght.Build.Editor
{
	public static class BuildReporter
	{
		[MenuItem("Build/Open Report")]
		public static void OpenReport()
		{
			if (!File.Exists(ReportParser.SummaryPath<ReportParserHtml>()))
			{
				Debug.LogError("Build Summary Missing. Maybe you haven't generated it yet?");
			}

			string path = Application.dataPath + "/../" + ReportParser.SummaryPath<ReportParserHtml>();
			Debug.Log(path);
			System.Diagnostics.Process.Start(path);
		}

		[MenuItem("Build/Generate Report")]
		public static void GenerateReport()
		{
			const string buildReportDir = "Assets/BuildReports";
			if (!Directory.Exists(buildReportDir))
			{
				Directory.CreateDirectory(buildReportDir);
			}

			DateTime date = File.GetLastWriteTime("Library/LastBuild.buildreport");
			string assetPath = buildReportDir + "/Build_" + date.ToString("yyyy-dd-MMM-HH-mm-ss") + ".buildreport";
			File.Copy("Library/LastBuild.buildreport", assetPath, true);
			AssetDatabase.ImportAsset(assetPath);
			BuildReport buildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(assetPath);
			ReportSummary(buildReport);
		}
		
		public static void ReportSummary(BuildReport report)
		{
			GeneratedReport newGeneratedReport = new GeneratedReport()
				.Add(BuildStatus(report))
				.Add(BuildSummary(report))
				.Add(BuildCommandArguments())
				.Add(BuildSettingsText(report))
				.Add(BuildStepsText(report));

			Directory.CreateDirectory("ReportSummary");
			
			ParsedReport parsedReportMarkdown = newGeneratedReport.Parse<ReportParserMarkdown>();
			File.WriteAllText(parsedReportMarkdown.SummaryPath(), parsedReportMarkdown.content);
			
			ParsedReport parsedReportHtml = newGeneratedReport.Parse<ReportParserHtml>();
			File.WriteAllText(parsedReportHtml.SummaryPath(), parsedReportHtml.content);
		}
		
		private static ReportSection BuildStatus(BuildReport report)
		{
			ReportSection reportSection = new();
			
			switch (report.summary.result)
			{
				case BuildResult.Succeeded:
					reportSection.Add(ReportPartType.Header1, "Build Succeeded! \u2705");
					break;
				case BuildResult.Failed:
					reportSection.Add(ReportPartType.Header1, "Build Failed! \ud83d\udfe5");
					break;
				case BuildResult.Cancelled:
					reportSection.Add(ReportPartType.Header1, "Build Cancelled! \ud83d\uded1");
					break;
				case BuildResult.Unknown:
				default:
					reportSection.Add(ReportPartType.Header1, "Build result is unknown! \ud83d\udfe5");
					break;
			}

			return reportSection;
		}
		
		private static ReportSection BuildSummary(BuildReport report)
		{
			BuildSummary summary = report.summary;
			
			return new ReportSection("Build Summary")
				.Add(ReportPartType.DotPoint, $"Result: {summary.result:G}")
				.Add(ReportPartType.DotPoint, $"Output Path: {summary.outputPath}")
				.Add(ReportPartType.DotPoint, $"File Size: {GetPathSizeInMegabytes(summary.outputPath):F2} MB")
				.Add(ReportPartType.DotPoint, $"Start Time: {summary.buildStartedAt:F}")
				.Add(ReportPartType.DotPoint, $"End Time: {summary.buildEndedAt:F}")
				.Add(ReportPartType.DotPoint, $"Duration: {summary.totalTime:g}")
				.Add(ReportPartType.DotPoint, $"Platform: {summary.platform:G}")
				.Add(ReportPartType.DotPoint, $"Release Stage: {(summary.options.HasFlag(BuildOptions.Development) ? "Development" : "Release")}");
		}
		
		private static ReportSection BuildCommandArguments()
		{
			Dictionary<string, string> commandArguments = CommandLineParser.ListCommandLineDisplayValues();

			ReportSection reportSection = new("Command Line Arguments");
			
			foreach (KeyValuePair<string, string> commandArgument in commandArguments)
			{
				reportSection.Add(new ReportPart(ReportPartType.DotPoint,
					string.IsNullOrWhiteSpace(commandArgument.Value)
						? $"Found flag {commandArgument.Key} with no value"
						: $"Found flag {commandArgument.Key} with value {commandArgument.Value}")
				);
			}

			return reportSection;
		}

		private static ReportSection BuildSettingsText(BuildReport report)
		{
			ReportSection reportSection = new ReportSection("Build Settings")
				.Add(ReportPartType.DotPoint, $"Build Target: {report.summary.platform}")
				.Add(ReportPartType.DotPoint, $"Build Flags: {report.summary.options:F}");
			
			switch (report.summary.platform)
			{
				case BuildTarget.StandaloneOSX:
					reportSection
						.Add(ReportPartType.DotPoint, $"Build Number: {PlayerSettings.macOS.buildNumber}");
					break;
				case BuildTarget.iOS:
					reportSection
						.Add(ReportPartType.DotPoint, $"Bundle Version: {PlayerSettings.bundleVersion}");
					break;
				case BuildTarget.Android:
					reportSection
						.Add(ReportPartType.DotPoint, $"Bundle Version: {PlayerSettings.bundleVersion}")
						.Add(ReportPartType.DotPoint, $"Bundle Version Code: {PlayerSettings.Android.bundleVersionCode}")
						.Add(ReportPartType.DotPoint, $"Build App Bundle: {EditorUserBuildSettings.buildAppBundle}")
						.Add(ReportPartType.DotPoint, $"Use Custom Keystore: {PlayerSettings.Android.useCustomKeystore}")
						.Add(ReportPartType.DotPoint, $"Keystore Name: {PlayerSettings.Android.keystoreName}")
						.Add(ReportPartType.DotPoint, $"Keystore Alias: {PlayerSettings.Android.keyaliasName}")
						.Add(ReportPartType.DotPoint, $"Target SDK Version: {PlayerSettings.Android.targetSdkVersion}");
					break;
			}
			
            return reportSection;
		}
		
		private static ReportSection BuildStepsText(BuildReport report)
		{
			List<ReportPart> reportParts = new();

			int steps = 0;
			int errors = 0;
			int asserts = 0;
			int warnings = 0;
			int exceptions = 0;

			foreach (BuildStep buildStep in report.steps)
			{
				reportParts.Add(new ReportPart(ReportPartType.DotPoint, $"{buildStep.name} - {buildStep.duration.TotalMilliseconds:F1}ms") {indentation = buildStep.depth});
				steps++;
				
				foreach (BuildStepMessage message in buildStep.messages)
				{
					switch (message.type)
					{
						case LogType.Error:
							reportParts.Add(new ReportPart(ReportPartType.DotPoint, $"[Error \ud83d\uded1] {message.content}") {indentation = buildStep.depth + 1});
							errors++;
							break;
						case LogType.Assert:
							reportParts.Add(new ReportPart(ReportPartType.DotPoint, $"[Assert \ud83d\uded1] {message.content}") {indentation = buildStep.depth + 1});
							asserts++;
							break;
						case LogType.Warning:
							reportParts.Add(new ReportPart(ReportPartType.DotPoint, $"[Warning \u26a0\ufe0f] {message.content}") {indentation = buildStep.depth + 1});
							warnings++;
							break;
						case LogType.Exception:
							reportParts.Add(new ReportPart(ReportPartType.DotPoint, $"[Exception \ud83d\uded1] {message.content}") {indentation = buildStep.depth + 1});
							exceptions++;
							break;
					}
				}
			}

			ReportSection reportSection = new ReportSection("Build Steps & Messages")
				.Add(ReportPartType.Header2, $"Messages [Count: {errors + asserts + warnings + exceptions}]")
				.Add(ReportPartType.DotPoint, $"Errors {errors}")
				.Add(ReportPartType.DotPoint, $"Asserts {asserts}")
				.Add(ReportPartType.DotPoint, $"Warnings {warnings}")
				.Add(ReportPartType.DotPoint, $"Exceptions {exceptions}")
				.Add(ReportPartType.Header2, $"Steps [Count: {steps}]");

			foreach (ReportPart reportPart in reportParts)
			{
				reportSection.Add(reportPart);
			}

			return reportSection;
		}

		private static double GetPathSizeInMegabytes(string path)
		{
			if (!File.Exists(path) && !Directory.Exists(path))
			{
				return 0;
			}

			// Check if the path is a file
			if (File.Exists(path))
			{
				return new FileInfo(path).Length / (1024.0 * 1024.0);
			}

			// If the path is a directory, calculate the total size of all files within the directory
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			long totalSize = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);

			// Convert bytes to megabytes
			return totalSize / (1024.0 * 1024.0);
		}
	}
}