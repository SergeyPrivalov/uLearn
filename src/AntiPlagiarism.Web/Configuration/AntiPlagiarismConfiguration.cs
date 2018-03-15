namespace AntiPlagiarism.Web.Configuration
{
	public class AntiPlagiarismConfiguration
	{
		public int SnippetTokensCount { get; set; }
		
		public int MaxCodeLength { get; set; }
		
		public StatisticsAnalyzingConfiguration StatisticsAnalyzing { get; set; }
		
		public PlagiarismDetectorConfiguration PlagiarismDetector { get; set; }
		
		public ActionsConfiguration Actions { get; set; }
	}

	public class StatisticsAnalyzingConfiguration
	{
		public int CountOfLastAuthorsForCalculatingMeanAndDeviation { get; set; }
		
		public int RecalculateStatisticsAfterSubmisionsCount { get; set; }
	}

	public class PlagiarismDetectorConfiguration
	{
		public int CountOfColdestSnippetsUsedToSearch { get; set; }
	}

	public class ActionsConfiguration
	{
		public GetAuthorPlagiarismsConfiguration GetAuthorPlagiarisms { get; set; }
	}
	
	public class GetAuthorPlagiarismsConfiguration
	{
		public int MaxLastSubmissionsCount { get; set; }
	}
}