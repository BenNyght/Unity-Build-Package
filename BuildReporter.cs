using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityBuilderAction
{
	public static class BuildReporter
	{
		private const string SummaryFilePath = "buildSummary.md";

		[MenuItem("Build/Open Report")]
		public static void OpenReport()
		{
			if (!File.Exists(SummaryFilePath))
			{
				Debug.LogError("Build Summary Missing. Maybe you haven't generated it yet?");
			}
			
			System.Diagnostics.Process.Start(SummaryFilePath);
		}
		
		public static void ReportSummary(BuildReport report)
		{
			StringBuilder output = new StringBuilder()
				.Append(BuildStatusText(report))
				.Append(BuildSummaryText(report))
				.Append(BuildCommandArgumentsText())
				.Append(BuildSettingsText(report))
				.Append(BuildStepsText(report));

			StringBuilder consoleOutput = new StringBuilder()
				.AppendLine($"{Environment.NewLine}")
				.AppendLine($"###########################{Environment.NewLine}")
				.AppendLine($"#      Build results      #{Environment.NewLine}")
				.AppendLine($"###########################{Environment.NewLine}")
				.AppendLine($"{Environment.NewLine}")
				.Append(output);
			
			Console.Write(consoleOutput);
			
			File.WriteAllText(SummaryFilePath, output.ToString());
		}

		private static StringBuilder BuildStatusText(BuildReport report)
		{
			StringBuilder statusText = new();
			
			switch (report.summary.result)
			{
				case BuildResult.Succeeded:
					statusText.AppendLine($"### Build Succeeded! \u2705");
					break;
				case BuildResult.Failed:
					statusText.AppendLine("### Build Failed! \ud83d\udfe5");
					break;
				case BuildResult.Cancelled:
					statusText.AppendLine("### Build Cancelled! \ud83d\uded1");
					break;
				case BuildResult.Unknown:
				default:
					statusText.AppendLine("### Build result is unknown! \ud83d\udfe5");
					break;
			}

			return statusText;
		}
		
		private static StringBuilder BuildSummaryText(BuildReport report)
		{
			BuildSummary summary = report.summary;

			return new StringBuilder()
				.AppendLine($"## Build Summary")
				.AppendLine($"- **Result:** {summary.result:G}")
				.AppendLine($"- **Output Path:** {summary.outputPath}")
				.AppendLine($"- **File Size:** {GetPathSizeInMegabytes(summary.outputPath):F2} MB")
				.AppendLine($"- **Start Time:** {summary.buildStartedAt:F}")
				.AppendLine($"- **End Time:** {summary.buildEndedAt:F}")
				.AppendLine($"- **Duration:** {summary.totalTime:g}")
				.AppendLine($"- **Platform:** {summary.platform:G}")
				.AppendLine($"- **Release Stage:** {(summary.options.HasFlag(BuildOptions.Development) ? "Development" : "Release")}");
		}
		
		private static StringBuilder BuildCommandArgumentsText()
		{
			Dictionary<string, string> commandArguments = CommandLineParser.ListCommandLineDisplayValues();
			StringBuilder commandArgumentsPrint = new StringBuilder().AppendLine($"## Command Line Arguments");
			
			foreach (KeyValuePair<string, string> commandArgument in commandArguments)
			{
				commandArgumentsPrint.AppendLine(
					string.IsNullOrWhiteSpace(commandArgument.Value)
						? $"- Found flag **{commandArgument.Key}** with no value"
						: $"- Found flag **{commandArgument.Key}** with value **{commandArgument.Value}**"
				);
			}

			return commandArgumentsPrint;
		}

		private static StringBuilder BuildSettingsText(BuildReport report)
		{
			StringBuilder settingsPrint = new StringBuilder()
				.AppendLine($"## Build Settings")
				.AppendLine($"- **Build Target:** {report.summary.platform}")
				.AppendLine($"- **Build Flags:** {report.summary.options:F}");
			
			switch (report.summary.platform)
			{
				case BuildTarget.StandaloneOSX:
					settingsPrint
						.AppendLine($"- **Build Number:** {PlayerSettings.macOS.buildNumber}");
					break;
				case BuildTarget.iOS:
					settingsPrint
						.AppendLine($"- **Bundle Version:** {PlayerSettings.bundleVersion}");
					break;
				case BuildTarget.Android:
					settingsPrint
						.AppendLine($"- **Bundle Version:** {PlayerSettings.bundleVersion}")
						.AppendLine($"- **Bundle Version Code:** {PlayerSettings.Android.bundleVersionCode}")
						.AppendLine($"- **Build App Bundle:** {EditorUserBuildSettings.buildAppBundle}")
						.AppendLine($"- **Use Custom Keystore:** {PlayerSettings.Android.useCustomKeystore}")
						.AppendLine($"- **Keystore Name:** {PlayerSettings.Android.keystoreName}")
						.AppendLine($"- **Keystore Alias:** {PlayerSettings.Android.keyaliasName}")
						.AppendLine($"- **Target SDK Version:** {PlayerSettings.Android.targetSdkVersion}");
					break;
			}
			
            return settingsPrint;
		}
		
		private static StringBuilder BuildStepsText(BuildReport report)
		{
			StringBuilder buildSteps = new();

			int steps = 0;
			int errors = 0;
			int asserts = 0;
			int warnings = 0;
			int exceptions = 0;

			foreach (BuildStep buildStep in report.steps)
			{
				buildSteps.AppendLine($"{GenerateTabs(buildStep.depth)}- {buildStep.name} - {buildStep.duration.TotalMilliseconds:F1}ms");
				steps++;
				
				string messageTab = GenerateTabs(buildStep.depth + 1);
				
				foreach (BuildStepMessage message in buildStep.messages)
				{
					switch (message.type)
					{
						case LogType.Error:
							buildSteps.AppendLine($"{messageTab}- [Error \ud83d\uded1] {message.content}");
							errors++;
							break;
						case LogType.Assert:
							buildSteps.AppendLine($"{messageTab}- [Assert \ud83d\uded1] {message.content}");
							asserts++;
							break;
						case LogType.Warning:
							buildSteps.AppendLine($"{messageTab}- [Warning \u26a0\ufe0f] {message.content}");
							warnings++;
							break;
						case LogType.Exception:
							buildSteps.AppendLine($"{messageTab}- [Exception \ud83d\uded1] {message.content}");
							exceptions++;
							break;
					}
				}
			}
			
			return new StringBuilder()
				.AppendLine($"## Build Steps & Messages")
				.AppendLine($"### Messages [Count: {errors + asserts + warnings + exceptions}]")
				.AppendLine($"- Errors {errors}")
				.AppendLine($"- Asserts {asserts}")
				.AppendLine($"- Warnings {warnings}")
				.AppendLine($"- Exceptions {exceptions}")
				.AppendLine($"### Steps [Count: {steps}]")
				.Append(buildSteps);
		}
		
		private static string GenerateTabs(int count)
		{
			return new string(' ', count * 4);
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
		
		private static string ToReadableString(TimeSpan span)
		{
			string formatted = string.Format("{0}{1}{2}{3}",
				span.Duration().Days > 0 ? $"{span.Days:0} day{(span.Days == 1 ? string.Empty : "s")}, " : string.Empty,
				span.Duration().Hours > 0 ? $"{span.Hours:0} hour{(span.Hours == 1 ? string.Empty : "s")}, " : string.Empty,
				span.Duration().Minutes > 0 ? $"{span.Minutes:0} minute{(span.Minutes == 1 ? string.Empty : "s")}, " : string.Empty,
				span.Duration().Seconds > 0 ? $"{span.Seconds:0} second{(span.Seconds == 1 ? string.Empty : "s")}" : string.Empty);

			if (formatted.EndsWith(", "))
			{
				formatted = formatted[..^2];
			}

			if (string.IsNullOrEmpty(formatted))
			{
				formatted = "0 seconds";
			}

			return formatted;
		}
	}
}