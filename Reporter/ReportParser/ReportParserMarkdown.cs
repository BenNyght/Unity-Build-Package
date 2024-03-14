using System;
using System.Linq;
using System.Text;

namespace BenNyght.Build.Editor
{
	public class ReportParserMarkdown : IReportParser
	{
		public ParsedReport Parse(GeneratedReport report)
		{
			StringBuilder stringBuilder = new();

			foreach (ReportPart reportPart in report.parts.SelectMany(reportSection => reportSection.parts))
			{
				switch (reportPart.type)
				{
					case ReportPartType.Header1:
						stringBuilder.AppendLine($"# {reportPart.content}");
						break;
					case ReportPartType.Header2:
						stringBuilder.AppendLine($"## {reportPart.content}");
						break;
					case ReportPartType.Header3:
						stringBuilder.AppendLine($"### {reportPart.content}");
						break;
					case ReportPartType.Body:
						stringBuilder.AppendLine($"{reportPart.content}");
						break;
					case ReportPartType.DotPoint:
						stringBuilder.AppendLine($"{GenerateTabs(reportPart.indentation)}- {reportPart.content}");
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return new ParsedReport(stringBuilder.ToString(), ".md", GetType());
		}

		public string GetReportPath()
		{
			return "buildSummary.md";
		}

		private static string GenerateTabs(int count)
		{
			return new string(' ', count * 4);
		}
	}
}