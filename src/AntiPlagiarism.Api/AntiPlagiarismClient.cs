using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.Api.Models.Results;
using Serilog.Core;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.Api
{
	public class AntiPlagiarismClient : IAntiPlagiarismClient
	{
		private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(10);

		private readonly Logger logger;
		private readonly string token;
		private readonly HttpClient httpClient;

		public AntiPlagiarismClient(string endpointUrl, string token, Logger logger)
		{
			this.logger = logger;
			this.token = token;

			httpClient = new HttpClient
			{
				BaseAddress = new Uri(endpointUrl),
				Timeout = defaultTimeout,
			};
		}

		private async Task<TResult> MakeRequestAsync<TParameters, TResult>(string url, TParameters parameters, HttpMethod method)
			where TParameters: ApiParameters
			where TResult: ApiResult
		{
			HttpResponseMessage response;
			logger.Information($"Send {method.Method} request to antiplagiarism service ({url}) with {parameters}");
			try
			{
				if (method == HttpMethod.Get)
					response = await httpClient.GetAsync(BuildUrl(httpClient.BaseAddress + url, token, parameters.ToNameValueCollection()));
				else if (method == HttpMethod.Post)
					response = await httpClient.PostAsJsonAsync(BuildUrl(httpClient.BaseAddress + url, token), parameters);
				else
					throw new AntiPlagiarismClientException($"Internal error: unsupported method for request: {method.Method}");
			}
			catch (Exception e)
			{
				logger.Error($"Can't send request to antiplagiarism service: {e.Message}");
				throw new AntiPlagiarismClientException($"Can't send request to antiplagiarism service: {e.Message}", e);
			}

			if (!response.IsSuccessStatusCode)
			{
				logger.Error($"Bad response code from antiplagiarism service: {(int)response.StatusCode} {response.StatusCode}");
				throw new AntiPlagiarismClientException($"Bad response code from antiplagiarism service: {(int)response.StatusCode} {response.StatusCode}");
			}

			TResult result;
			try
			{
				result = await response.Content.ReadAsJsonAsync<TResult>();
			}
			catch (Exception e)
			{
				logger.Error($"Can't parse response from antiplagiarism service: {e.Message}");
				throw new AntiPlagiarismClientException($"Can't parse response from antiplagiarism service: {e.Message}", e);
			}
			
			logger.Information($"Received response from antiplagiarism service: {result}");
			return result;
		}

		private static Uri BuildUrl(string baseUrl, string token, NameValueCollection parameters=null)
		{
			if (parameters == null)
				parameters = new NameValueCollection();
			
			var builder = new UriBuilder(baseUrl);
			var queryString = HttpUtility.ParseQueryString(builder.Query);
			queryString["token"] = token;
			foreach (var parameterName in parameters.AllKeys)
				queryString[parameterName] = parameters[parameterName];

			builder.Query = queryString.ToQueryString();
			return builder.Uri;
		}

		public Task<AddSubmissionResult> AddSubmissionAsync(AddSubmissionParameters parameters)
		{
			return MakeRequestAsync<AddSubmissionParameters, AddSubmissionResult>(Urls.AddSubmission, parameters, HttpMethod.Post);
		}

		public Task<GetSubmissionPlagiarismsResult> GetSubmissionPlagiarismsAsync(GetSubmissionPlagiarismsParameters parameters)
		{
			return MakeRequestAsync<GetSubmissionPlagiarismsParameters, GetSubmissionPlagiarismsResult>(Urls.GetSubmissionPlagiarisms, parameters, HttpMethod.Get);
		}
		
		public Task<GetAuthorPlagiarismsResult> GetAuthorPlagiarismsAsync(GetAuthorPlagiarismsParameters parameters)
		{
			return MakeRequestAsync<GetAuthorPlagiarismsParameters, GetAuthorPlagiarismsResult>(Urls.GetAuthorPlagiarisms, parameters, HttpMethod.Get);
		}
	}
}