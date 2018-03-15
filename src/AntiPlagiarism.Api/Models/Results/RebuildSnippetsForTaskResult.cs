using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AntiPlagiarism.Api.Models.Results
{
	[DataContract]
	public class RebuildSnippetsForTaskResult : ApiSuccessResult
	{
		[DataMember(Name = "submissions_ids")]
		public List<int> SubmissionsIds { get; set; }
	}
}