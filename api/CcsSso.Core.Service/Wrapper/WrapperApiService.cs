using CcsSso.Core.Domain.Contracts.Wrapper;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Exceptions;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.Wrapper
{
	public class WrapperApiService : IWrapperApiService
	{
		private readonly IHttpClientFactory _httpClientFactory;

		public WrapperApiService(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		public async Task<T> GetAsync<T>(WrapperApi wrapperApi, string url, string cacheKey, string errorMessage)
		{
			var client = GetHttpClient(wrapperApi);

			var response = await client.GetAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var result = JsonConvert.DeserializeObject<T>(responseString);
				return result;
			}
			else if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new ResourceNotFoundException();
			}
			else if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				throw new CcsSsoException(responseString);
			}
			else
			{
				throw new CcsSsoException(errorMessage);
			}
		}

		public async Task<T> PostAsync<T>(WrapperApi wrapperApi, string url, object requestData, string errorMessage)
		{
			var client = GetHttpClient(wrapperApi);

			HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
			{ ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

			var response = await client.PostAsync(url, data);
			var responseString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var result = JsonConvert.DeserializeObject<T>(responseString);
				return result;
			}
			else if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new ResourceNotFoundException();
			}
			else if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				throw new CcsSsoException(responseString);
			}
			else
			{
				throw new CcsSsoException(errorMessage);
			}
		}
		public async Task<bool> DeleteAsync(WrapperApi wrapperApi, string url, string errorMessage)
		{
			var client = GetHttpClient(wrapperApi);

			var response = await client.DeleteAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				return true;
			}
			else if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new ResourceNotFoundException();
			}
			else if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				throw new CcsSsoException(responseString);
			}
			else
			{
				throw new CcsSsoException(errorMessage);
			}
		}

		public async Task PutAsync(WrapperApi wrapperApi, string url, object requestData, string errorMessage)
		{
			var client = GetHttpClient(wrapperApi);

			HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
			{ ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

			var response = await client.PutAsync(url, data);
			var responseString = await response.Content.ReadAsStringAsync();
			if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				throw new CcsSsoException(responseString);
			}
			else if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new ResourceNotFoundException();
			}
			else if (!response.IsSuccessStatusCode)
			{
				throw new CcsSsoException(errorMessage);
			}
		}
		public async Task<T> PutAsync<T>(WrapperApi wrapperApi, string url, object requestData, string errorMessage)
		{
			var client = GetHttpClient(wrapperApi);

			HttpContent data = new StringContent(JsonConvert.SerializeObject(requestData, new JsonSerializerSettings
			{ ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");

			var response = await client.PutAsync(url, data);
			var responseString = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<T>(responseString);
			if (response.IsSuccessStatusCode)
			{
				return result;
			}
			else if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				throw new CcsSsoException(responseString);
			}
			else if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new ResourceNotFoundException();
			}
			else if (!response.IsSuccessStatusCode)
			{
				throw new CcsSsoException(errorMessage);
			}
			return result;
		}
		public async Task<T> DeleteAsync<T>(WrapperApi wrapperApi, string url, string errorMessage)
		{
			var client = GetHttpClient(wrapperApi);

			var response = await client.DeleteAsync(url);
			var responseString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var result = JsonConvert.DeserializeObject<T>(responseString);
				return result;
			}
			else if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new ResourceNotFoundException();
			}
			else if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				throw new CcsSsoException(responseString);
			}
			else
			{
				throw new CcsSsoException(errorMessage);
			}
		}

		private HttpClient GetHttpClient(WrapperApi wrapperApi)
		{
			var clientName = wrapperApi switch
			{
				WrapperApi.Organisation => "OrgWrapperApi",
                WrapperApi.OrganisationDelete => "OrgWrapperDeleteApi",
                WrapperApi.Configuration => "ConfigWrapperApi",
				WrapperApi.Contact => "ContactWrapperApi",
        WrapperApi.ContactDelete => "ContactWrapperDeleteApi",
        WrapperApi.User => "UserWrapperApi",
				_ => "SecurityWrapperApi"
			};
			var client = _httpClientFactory.CreateClient(clientName);
			return client;
		}

	}
}

