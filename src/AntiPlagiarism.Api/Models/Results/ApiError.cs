using System.Runtime.Serialization;

namespace AntiPlagiarism.Api.Models.Results
{
	[DataContract]
	public class ApiError : ApiResult
	{
		[DataMember(Name = "error")]
		public string Error { get; set; }

		public ApiError()
		{
		}
		
		public ApiError(string error)
		{
			Status = ApiResultStatus.Error;
			Error = error;
		}

		/* TODO (andgein): remove this method and replace it with constructor */
		public static ApiError Create(string error)
		{
			return new ApiError(error);
		}
	}
}