using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.Api.Models.Results
{
	[DataContract]
	public abstract class ApiResult
	{
		[DataMember(Name = "status")]
		[JsonConverter(typeof(StringEnumConverter), true)]
		public ApiResultStatus Status { get; set; }

		public override string ToString()
		{
			return this.JsonSerialize();
		}
	}
}