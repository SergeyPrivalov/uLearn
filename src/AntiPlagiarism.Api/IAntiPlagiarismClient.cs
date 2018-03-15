using System.Net.Http;
using System.Threading.Tasks;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.Api.Models.Results;

namespace AntiPlagiarism.Api
{
	public interface IAntiPlagiarismClient
	{
		Task<AddSubmissionResult> AddSubmissionAsync(AddSubmissionParameters parameters);
		Task<GetSubmissionPlagiarismsResult> GetSubmissionPlagiarismsAsync(GetSubmissionPlagiarismsParameters parameters);
		Task<GetAuthorPlagiarismsResult> GetAuthorPlagiarismsAsync(GetAuthorPlagiarismsParameters parameters);
	}
}