using System;
using System.Collections.Generic;

namespace BenNyght.Build.Editor
{
	[System.Serializable]
	public class GeneratedReport
	{
		public List<ReportSection> parts = new List<ReportSection>();
		public DateTime utcDateCreated = DateTime.UtcNow;
		public DateTime localDateCreated = DateTime.Now;

		public GeneratedReport Add(ReportSection reportSection)
		{
			parts.Add(reportSection);
			return this;
		}
	}
	
	[System.Serializable]
	public class ReportSection
	{
		public List<ReportPart> parts = new List<ReportPart>();

		public ReportSection Add(ReportPart part)
		{
			parts.Add(part);
			return this;
		}

		public ReportSection Add(ReportPartType type, string content)
		{
			return Add(new ReportPart(type, content));
		}

		public ReportSection(string title)
		{
			parts.Add(new ReportPart(ReportPartType.Header1, title));
		}
		
		public ReportSection() {}
	}

	[System.Serializable]
	public class ReportPart
	{
		public ReportPartType type;
		public string content;
		public int indentation;

		public ReportPart(ReportPartType type, string content)
		{
			this.type = type;
			this.content = content;
		}
	}

	public enum ReportPartType
	{
		Header1,
		Header2,
		Header3,
		Body,
		DotPoint,
	}
}