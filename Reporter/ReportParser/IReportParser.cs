namespace BenNyght.Build.Editor
{
	public interface IReportParser
	{
		public ParsedReport Parse(GeneratedReport report);

		public string GetReportPath();
	}
}