using System;

namespace BenNyght.Build.Editor
{
	[System.Serializable]
	public class ParsedReport
	{
		public string content;
		public string fileExtensions;
		public Type parserType;
		
		public ParsedReport(string content, string fileExtensions, Type parserType)
		{
			this.content = content;
			this.fileExtensions = fileExtensions;
			this.parserType = parserType;
		}
	}
	
	public static class ReportParser
	{
		public static ParsedReport Parse<T>(this GeneratedReport report) where T : IReportParser
		{
			IReportParser reporter = Activator.CreateInstance<T>();
			return reporter.Parse(report);
		}

		public static string SummaryPath(this ParsedReport parsedReport)
		{
			IReportParser reporter = (IReportParser)Activator.CreateInstance(parsedReport.parserType);
			return "ReportSummary/" + reporter.GetReportPath();
		}
		
		public static string SummaryPath<T>() where T : IReportParser
		{
			IReportParser reporter = Activator.CreateInstance<T>();
			return "ReportSummary/" + reporter.GetReportPath();
		}
	}
}