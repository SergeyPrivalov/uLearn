using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;
using log4net;
using RunCsJob.Api;
using uLearn;
using uLearn.Extensions;
using Ulearn.Common.Extensions;

namespace RunCsJob
{
	internal class Client
	{
		private readonly string token;
		private readonly HttpClient httpClient;
		private const string instanceIdEnvironmentVariableName = "WEBSITE_INSTANCE_ID";
		private const string arrAffinityCookieName = "ARRAffinity";
		private static readonly ILog log = LogManager.GetLogger(typeof(Client));

		public Client(string address, string token)
		{
			this.token = token;

			var cookieContainer = new CookieContainer();
			var httpClientHandler = new HttpClientHandler { CookieContainer = cookieContainer };
			var baseAddress = new Uri(address + "/");

			httpClient = new HttpClient(httpClientHandler) { BaseAddress = baseAddress };
			httpClient.DefaultRequestHeaders.Accept.Clear();
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			/* Select instanceId for multiple azure instances */
			var instanceId = Environment.GetEnvironmentVariable(instanceIdEnvironmentVariableName);
			if (!string.IsNullOrEmpty(instanceId))
				cookieContainer.Add(baseAddress, new Cookie(arrAffinityCookieName, instanceId));
		}

		public async Task<List<RunnerSubmission>> TryGetSubmissions(int threadsCount)
		{
			var uri = GetUri("GetSubmissions", new[] { "count", threadsCount.ToString(CultureInfo.InvariantCulture) });
			try
			{
				log.Info($"Отправляю запрос на {uri}");
				var response = await httpClient.GetAsync(uri);
				log.Info($"Получил ответ, код {(int) response.StatusCode} {response.StatusCode}, читаю содержимое");
				if (response.IsSuccessStatusCode)
					return await response.Content.ReadAsJsonAsync<List<RunnerSubmission>>(JsonConfig.GetSettings());
				else
				{
					var text = await response.Content.ReadAsStringAsync();
					log.Error($"StatusCode {response.StatusCode}\n{text}");
				}
			}
			catch (Exception e)
			{
				log.Error($"Не могу подключиться к {httpClient.BaseAddress}{uri}", e);
				if (e.InnerException != null)
					log.Error(e.InnerException.Message);
			}
			return new List<RunnerSubmission>();
		}

		public async void SendResults(List<RunningResults> results)
		{
			var uri = GetUri("PostResults");
			var response = await httpClient.PostAsJsonAsync(uri, results);

			if (response.IsSuccessStatusCode)
				return;

			log.Error($"Не могу отправить результаты проверки (они ниже) на сервер: {response}\n{response.Content.ReadAsStringAsync().Result}");
			foreach (var result in results)
			{
				log.Info($"Результат: {result}");
			}
		}

		private string GetUri(string path, params string[][] parameters)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["token"] = token;
			foreach (var parameter in parameters)
			{
				query[parameter[0]] = parameter[1];
			}
			return $"{path}/?{query}";
		}
	}
}