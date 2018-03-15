using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GiftsGranter
{
	public class StaffClient
	{
		private readonly string clientAuth;
		private string authToken;
		private string passportUri = "https://passport.skbkontur.ru/authz/staff/oauth/token";

		/// <param name="clientAuth">format: "clientId:clientSecret"</param>
		public StaffClient(string clientAuth)
		{
			this.clientAuth = clientAuth;
		}

		private JObject Get(string url)
		{
			var client = CreateHttpClient();
			var response = client.GetAsync($"https://staff.skbkontur.ru/api/{url}").Result;
			return GetJsonResponse(response);
		}

		private HttpClient CreateHttpClient()
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
			return client;
		}

		private JObject Post(string url, JObject jsonContent)
		{
			var client = CreateHttpClient();
			var content = new StringContent(JsonConvert.SerializeObject(jsonContent), Encoding.UTF8, "application/json");
			var response = client.PostAsync($"https://staff.skbkontur.ru/api/{url}", content).Result;
			return GetJsonResponse(response);
		}

		public JObject GetUser(string sid)
		{
			return Get($"users/getBySid?sid={sid}");
		}

		public JObject GetUserGifts(int staffUserId)
		{
			return Get($"feed/user_{staffUserId}?filter=gift");
		}

		public JObject GrantGift(int staffUserId, int score, CourseSettings courseSettings)
		{
			var gift = GiftsFactory.CreateGift(score, courseSettings);
			return Post($"feed/user_{staffUserId}", gift);
		}

		public string GetRefreshToken(string username, string password)
		{
			var content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				{ "grant_type", "password" },
				{ "username", username },
				{ "password", password },
				{ "scope", "profiles"},
			});
			var client = new HttpClient();
			AddClientAuthorizationHeader(client);
			var response = client.PostAsync(passportUri, content).Result;
			var responseJson = GetJsonResponse(response);
			return responseJson["refresh_token"].Value<string>();
		}

		private static JObject GetJsonResponse(HttpResponseMessage response)
		{
			string result = response.Content.ReadAsStringAsync().Result;
			if (response.StatusCode != HttpStatusCode.OK)
				throw new Exception(result);
			return JObject.Parse(result);
		}

		public void UseRefreshToken(string refreshToken)
		{
			var client = new HttpClient();
			var content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				{ "grant_type", "refresh_token" },
				{ "refresh_token", refreshToken }
			});
			AddClientAuthorizationHeader(client);
			var response = client.PostAsync(passportUri, content).Result;
			var responseJson = GetJsonResponse(response);
			authToken = responseJson["access_token"].Value<string>();
		}

		private void AddClientAuthorizationHeader(HttpClient client)
		{
			string base64ClientAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientAuth));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64ClientAuth);
		}
	}
}