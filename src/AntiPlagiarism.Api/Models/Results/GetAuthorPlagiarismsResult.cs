using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AntiPlagiarism.Api.Models.Results
{
	[DataContract]
	public class GetAuthorPlagiarismsResult : ApiSuccessResult
	{
		[DataMember(Name = "submissions")]
		public List<ResearchedSubmission> ResearchedSubmissions { get; set; }
		
		[DataMember(Name = "suspicion_levels")]
		public SuspicionLevels SuspicionLevels { get; set; }		

		public GetAuthorPlagiarismsResult()
		{
			ResearchedSubmissions = new List<ResearchedSubmission>();
		}
	}

	[DataContract]
	public class ResearchedSubmission
	{
		[DataMember(Name = "submission")]
		public SubmissionInfo SubmissionInfo { get; set; }
		
		[DataMember(Name = "plagiarisms")]
		public List<Plagiarism> Plagiarisms { get; set; }
		
		[DataMember(Name = "tokens_positions")]
		public List<TokenPosition> TokensPositions { get; set; }
		
		[DataMember(Name = "analyzed_code_units")]
		public List<AnalyzedCodeUnit> AnalyzedCodeUnits { get; set; }
	}
}