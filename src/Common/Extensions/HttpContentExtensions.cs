using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ulearn.Common.Extensions
{
	public static class HttpContentExtensions
	{
		public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content, JsonSerializerSettings jsonSerializerSettings=null)
		{
			if (jsonSerializerSettings == null)
				jsonSerializerSettings = new JsonSerializerSettings();
			
			var s = await content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(s, jsonSerializerSettings);
		}
	}
}