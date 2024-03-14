using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BenNyght.Utility.Editor
{
	public static class CommandLineParser
	{
		private static readonly string Eol = Environment.NewLine;
		private static readonly string[] Secrets = { "androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass", "accessToken", "licensingIpc", "hubSessionId", "androidKeystoreName" };

		public static Dictionary<string, string> ParseCommandLineArguments()
		{
			return GetCommandLineArguments()
				.ToDictionary(
					commandLineArgument => commandLineArgument.Key, 
					commandLineArgument => commandLineArgument.Value.value);
			
		}
		
		public static Dictionary<string, string> ListCommandLineDisplayValues()
		{
			return GetCommandLineArguments()
				.ToDictionary(
					commandLineArgument => commandLineArgument.Key, 
					commandLineArgument => commandLineArgument.Value.displayValue);
		}

		private static Dictionary<string, (string value, string displayValue)> GetCommandLineArguments()
		{
			Dictionary<string, (string value, string displayValue)> providedArguments = new();
			string[] args = Environment.GetCommandLineArgs();

			Console.WriteLine(
				$"{Eol}" +
				$"###########################{Eol}" +
				$"#    Parsing settings     #{Eol}" +
				$"###########################{Eol}" +
				$"{Eol}"
			);

			// Extract flags with optional values
			for (int current = 0, next = 1; current < args.Length; current++, next++)
			{
				// Parse flag
				bool isFlag = args[current].StartsWith("-");
				if (!isFlag) continue;
				string flag = args[current].TrimStart('-');

				// Parse optional value
				bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
				string value = flagHasValue ? args[next].TrimStart('-') : "";
				bool secret = Secrets.Contains(flag);
				string displayValue = secret ? "*HIDDEN*" : value;

				// Assign
				Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
				providedArguments.Add(flag, (value, displayValue));
			}

			return providedArguments;
		}
		
		public static Dictionary<string, string> GetValidatedOptions()
		{
			Dictionary<string, string> commandArguments = ParseCommandLineArguments();

			if (!commandArguments.TryGetValue("projectPath", out string _))
			{
				Console.WriteLine("Missing argument -projectPath");
				EditorApplication.Exit(110);
			}

			if (!commandArguments.TryGetValue("buildTarget", out string buildTarget))
			{
				Console.WriteLine("Missing argument -buildTarget");
				EditorApplication.Exit(120);
			}

			if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
			{
				Console.WriteLine($"{buildTarget} is not a defined {nameof(BuildTarget)}");
				EditorApplication.Exit(121);
			}

			if (!commandArguments.TryGetValue("customBuildPath", out string _))
			{
				Console.WriteLine("Missing argument -customBuildPath");
				EditorApplication.Exit(130);
			}

			const string defaultCustomBuildName = "TestBuild";
			if (!commandArguments.TryGetValue("customBuildName", out string customBuildName))
			{
				Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
				commandArguments.Add("customBuildName", defaultCustomBuildName);
			}
			else if (customBuildName == "")
			{
				Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
				commandArguments.Add("customBuildName", defaultCustomBuildName);
			}

			return commandArguments;
		}
	}
}