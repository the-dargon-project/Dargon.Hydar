using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dargon.Platform.Client {
   public class WyvernResponse<TResult> {
      public bool Success { get; set; }
      public int ResponseCode { get; set; }
      public TResult Result { get; set; }
      public dynamic Error { get; set; }
   }

   public class AuthenticationResult {
      public Guid AccountId { get; set; }
      public string AccessToken { get; set; }
   }

   public interface WyvernClientState {
      string AccessToken { get; set; }
   }

   public class WyvernClientStateImpl : WyvernClientState {
      public string AccessToken { get; set; }
   }

   public class WyvernClientImpl : WyvernClient {
      private readonly HttpClient httpClient = new HttpClient();
      private readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings {
         ContractResolver = new CamelCasePropertyNamesContractResolver()
      };
      private readonly WyvernClientConfiguration configuration;
      private readonly WyvernClientState state;

      public WyvernClientImpl(WyvernClientConfiguration configuration, WyvernClientState state = null) {
         this.configuration = configuration;
         this.state = state ?? new WyvernClientStateImpl();
      }

      public WyvernClientConfiguration Configuration => configuration;
      public WyvernClientState State => state;

      public void LogIn(string username, string password) {
         state.AccessToken = null;

         var response = PostAsync<AuthenticationResult>("/login", new { username = username, password = password }).Result;
         if (response.Success) {
            Console.WriteLine("Set access token to " + response.Result.AccessToken);
            state.AccessToken = response.Result.AccessToken;
         } else {
            throw new Exception(JsonConvert.SerializeObject(response, jsonSerializerSettings));
         }
      }

      public void UploadLogs(string logsZipArchivePath) {
         var response = PostFilesAsync<dynamic>("/feedback/client-logs", logsZipArchivePath).Result;
         if (!response.Success) {
            throw new Exception(JsonConvert.SerializeObject(response, jsonSerializerSettings));
         }
      }

      private Task<WyvernResponse<TResult>> PostAsync<TResult>(string relativeUrl, object body) {
         var bodyJson = JsonConvert.SerializeObject(body, jsonSerializerSettings);
         return SendAsync<WyvernResponse<TResult>>(relativeUrl, HttpMethod.Post, CreateJsonHttpContent(bodyJson));
      }

      private Task<WyvernResponse<TResult>> PostFilesAsync<TResult>(string relativeUrl, params string[] filePaths) {
         return SendAsync<WyvernResponse<TResult>>(relativeUrl, HttpMethod.Post, CreateFileUploadHttpContent(filePaths));
      }

      private async Task<TResult> SendAsync<TResult>(string relativeUrl, HttpMethod method, HttpContent content) {
         var url = BuildUrl(relativeUrl);
         var request = ConfigureRequest(new HttpRequestMessage {
            RequestUri = new Uri(url),
            Method = HttpMethod.Post,
            Content = content
         });
         Console.WriteLine($"{method} {url}.");
         var response = await httpClient.SendAsync(request);
         var responseJson = await response.Content.ReadAsStringAsync();
         Console.WriteLine("Response: " + responseJson);
         Console.WriteLine();
         return JsonConvert.DeserializeObject<TResult>(responseJson, jsonSerializerSettings);
      }

      private string BuildUrl(string relativeUrl) {
         return Configuration.ApiV1Base + relativeUrl;
      }

      private HttpContent CreateJsonHttpContent(string json) {
         return new StringContent(json, Encoding.UTF8, "application/json");
      }

      private HttpContent CreateFileUploadHttpContent(string[] filePaths) {
         var httpContent = new MultipartFormDataContent();
         foreach (var filePath in filePaths) {
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") {
               FileName = new FileInfo(filePath).Name
            };
            httpContent.Add(fileContent);
         }
         return httpContent;
      }

      private HttpRequestMessage ConfigureRequest(HttpRequestMessage request) {
         if (!string.IsNullOrWhiteSpace(state.AccessToken)) {
            request.Headers.Add("Authorization", "Bearer " + state.AccessToken);
         }
         request.Headers.Add("X-Dargon-Client-Id", Configuration.ClientId.ToString("n"));
         request.Headers.Add("X-Dargon-Client-Full-Name", Configuration.ClientName + " " + Configuration.ClientVersion);
         return request;
      }
   }
}