using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace IoTHubReader.Shared
{
	public partial class DeviceTemplatesClient
	{
		private readonly HttpClient _httpClient;

		public string BaseUrl { get; set; }

		public DeviceTemplatesClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public Task<DeviceTemplateCollection> ListAsync()
		{
			return ListAsync(CancellationToken.None);
		}

		public Task<DeviceTemplateCollection> ListAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(new DeviceTemplateCollection());
		}

		public Task<DTCapabilityModel> SetAsync(DTCapabilityModel body, string device_template_id)
		{
			return SetAsync(body, device_template_id, CancellationToken.None);
		}

		public async Task<DTCapabilityModel> SetAsync(DTCapabilityModel body, string device_template_id, CancellationToken cancellationToken)
		{
			if (device_template_id == null)
				throw new ArgumentNullException("device_template_id");

			var urlBuilder_ = new StringBuilder();
			urlBuilder_.Append(BaseUrl != null ? BaseUrl.TrimEnd('/') : "").Append("/deviceTemplates/{device_template_id}");
			urlBuilder_.Replace("{device_template_id}", Uri.EscapeDataString(ConvertToString(device_template_id, CultureInfo.InvariantCulture)));

			var client_ = _httpClient;
			try {
				using (var request_ = new HttpRequestMessage()) {
					var content_ = new StringContent(JsonSerializer.Serialize(body));
					content_.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
					request_.Content = content_;
					request_.Method = new HttpMethod("PUT");
					request_.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

					PrepareRequest(client_, request_, urlBuilder_);
					var url_ = urlBuilder_.ToString();
					request_.RequestUri = new Uri(url_, UriKind.RelativeOrAbsolute);
					PrepareRequest(client_, request_, url_);

					var response_ = await client_.SendAsync(request_, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
					try {
						var headers_ = Enumerable.ToDictionary(response_.Headers, h_ => h_.Key, h_ => h_.Value);
						if (response_.Content != null && response_.Content.Headers != null) {
							foreach (var item_ in response_.Content.Headers)
								headers_[item_.Key] = item_.Value;
						}

						ProcessResponse(client_, response_);

						var status_ = ((int)response_.StatusCode).ToString();
						if (status_ == "200") {
							var objectResponse_ = await ReadObjectResponseAsync<DTCapabilityModel>(response_, headers_).ConfigureAwait(false);
							return objectResponse_.Object;
						}
						else
						if (status_ != "200" && status_ != "204") {
							var responseData_ = response_.Content == null ? null : await response_.Content.ReadAsStringAsync().ConfigureAwait(false);
							throw new ApiException("The HTTP status code of the response was not expected (" + (int)response_.StatusCode + ").", (int)response_.StatusCode, responseData_, headers_, null);
						}

						return default(DTCapabilityModel);
					}
					finally {
						if (response_ != null)
							response_.Dispose();
					}
				}
			}
			finally {
			}
		}

		partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);
		partial void PrepareRequest(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder);
		partial void ProcessResponse(HttpClient client, HttpResponseMessage response);

		protected struct ObjectResponseResult<T>
		{
			public ObjectResponseResult(T responseObject, string responseText)
			{
				Object = responseObject;
				Text = responseText;
			}

			public T Object { get; }

			public string Text { get; }
		}

		public bool ReadResponseAsString { get; set; }

		protected virtual async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers)
		{
			if (response == null || response.Content == null) {
				return new ObjectResponseResult<T>(default(T), string.Empty);
			}

			if (ReadResponseAsString) {
				var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				try {
					var typedBody = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseText);
					return new ObjectResponseResult<T>(typedBody, responseText);
				}
				catch (Newtonsoft.Json.JsonException exception) {
					var message = "Could not deserialize the response body string as " + typeof(T).FullName + ".";
					throw new ApiException(message, (int)response.StatusCode, responseText, headers, exception);
				}
			}
			else {
				try {
					using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
					using (var streamReader = new StreamReader(responseStream))
					using (var jsonTextReader = new Newtonsoft.Json.JsonTextReader(streamReader)) {
						var serializer = Newtonsoft.Json.JsonSerializer.Create();
						var typedBody = serializer.Deserialize<T>(jsonTextReader);
						return new ObjectResponseResult<T>(typedBody, string.Empty);
					}
				}
				catch (Newtonsoft.Json.JsonException exception) {
					var message = "Could not deserialize the response body stream as " + typeof(T).FullName + ".";
					throw new ApiException(message, (int)response.StatusCode, string.Empty, headers, exception);
				}
			}
		}

		private string ConvertToString(object value, CultureInfo cultureInfo)
		{
			if (value is Enum) {
				var name = Enum.GetName(value.GetType(), value);
				if (name != null) {
					var field = IntrospectionExtensions.GetTypeInfo(value.GetType()).GetDeclaredField(name);
					if (field != null) {
						var attribute = CustomAttributeExtensions.GetCustomAttribute(field, typeof(EnumMemberAttribute))
							as EnumMemberAttribute;
						if (attribute != null) {
							return attribute.Value != null ? attribute.Value : name;
						}
					}
				}
			}
			else if (value is bool) {
				return Convert.ToString(value, cultureInfo).ToLowerInvariant();
			}
			else if (value is byte[]) {
				return Convert.ToBase64String((byte[])value);
			}
			else if (value != null && value.GetType().IsArray) {
				var array = Enumerable.OfType<object>((Array)value);
				return string.Join(",", Enumerable.Select(array, o => ConvertToString(o, cultureInfo)));
			}

			return Convert.ToString(value, cultureInfo);
		}
	}

	public class DeviceTemplateCollection
	{
		public ICollection<DTCapabilityModel> Value { get; set; } = new Collection<DTCapabilityModel>();
	}

	public partial class ApiException : Exception
	{
		public int StatusCode { get; private set; }

		public string Response { get; private set; }

		public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

		public ApiException(string message, int statusCode, string response, IReadOnlyDictionary<string, IEnumerable<string>> headers, Exception innerException)
			: base(message + "\n\nStatus: " + statusCode + "\nResponse: \n" + response.Substring(0, response.Length >= 512 ? 512 : response.Length), innerException)
		{
			StatusCode = statusCode;
			Response = response;
			Headers = headers;
		}

		public override string ToString()
		{
			return string.Format("HTTP Response: \n\n{0}\n\n{1}", Response, base.ToString());
		}
	}
}
