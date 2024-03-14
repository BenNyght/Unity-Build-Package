using System;
using System.Linq;
using System.Text;

namespace BenNyght.Build.Editor
{
	public class ReportParserHtml : IReportParser
	{
		public ParsedReport Parse(GeneratedReport report)
		{
			StringBuilder stringBuilder = new();

			stringBuilder.Append(
				"<!DOCTYPE html>" +
				"\n<html>" +
				"\n<head>" +
				"\n<title>Unity Build Report</title>" +
				"\n</head>" +
				"\n<body>"
				);

			foreach (ReportPart reportPart in report.parts.SelectMany(reportSection => reportSection.parts))
			{
				switch (reportPart.type)
				{
					case ReportPartType.Header1:
						stringBuilder.AppendLine($"<h1>{reportPart.content}</h1>");
						break;
					case ReportPartType.Header2:
						stringBuilder.AppendLine($"<h2>{reportPart.content}</h2>");
						break;
					case ReportPartType.Header3:
						stringBuilder.AppendLine($"<h3>{reportPart.content}</h3>");
						break;
					case ReportPartType.Body:
						stringBuilder.AppendLine($"<p>{reportPart.content}</p>");
						break;
					case ReportPartType.DotPoint:
						CreateIndentation(stringBuilder, reportPart);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			stringBuilder.Append(
				"</body>\n" +
				"</html>"
				);

			return new ParsedReport(stringBuilder.ToString(), ".html", GetType());
		}

		private void CreateIndentation(StringBuilder stringBuilder, ReportPart reportPart)
		{
			stringBuilder.AppendLine(GenerateTabs(reportPart.indentation,"<ul>"));
			stringBuilder.AppendLine("<li>");
			stringBuilder.AppendLine(reportPart.content);
			stringBuilder.AppendLine("</li>");
			stringBuilder.AppendLine(GenerateTabs(reportPart.indentation,"</ul>"));
		}

		public string GetReportPath()
		{
			return "buildSummary.html";
		}

		private static string GenerateTabs(int count, string style)
		{
			string tabs = "";
			for (int i = 0; i < count; i++)
			{
				tabs += style;
			}
			return tabs;
		}
	}
}