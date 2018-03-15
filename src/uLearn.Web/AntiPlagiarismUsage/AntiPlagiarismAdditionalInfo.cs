using System.Runtime.Serialization;

namespace uLearn.Web.AntiPlagiarismUsage
{
	[DataContract]
	public class AntiPlagiarismAdditionalInfo
	{
		[DataMember]
		public int SubmissionId { get; set; }
	}
}