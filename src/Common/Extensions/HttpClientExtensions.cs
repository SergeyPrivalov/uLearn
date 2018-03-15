using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ulearn.Common.Extensions
{
	public static class HttpClientExtensions
	{
		public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string uri, T payload)
		{
			var serializedPayload = JsonConvert.SerializeObject(payload, Formatting.Indented);
			return await client.PostAsync(
				uri,
				new StringContent(serializedPayload, Encoding.UTF8, "application/json"),
				CancellationToken.None);
		}

		public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, Uri uri, T payload)
		{
			return client.PostAsJsonAsync(uri.ToString(), payload);
		}

		public static async Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string uri, T payload)
		{
			var serializedPayload = JsonConvert.SerializeObject(payload, Formatting.Indented);
			return await client.PutAsync(
				uri,
				new StringContent(serializedPayload, Encoding.UTF8, "application/json"),
				CancellationToken.None);
		}
		
		public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, Uri uri, T payload)
		{
			return client.PutAsJsonAsync(uri.ToString(), payload);
		}
	}
}