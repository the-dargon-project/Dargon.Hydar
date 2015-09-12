using Nancy;
using Nancy.Responses;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dargon.Platform.Common;
using Nancy.ModelBinding;

namespace Dargon.Platform.Webend {
   public abstract class WebApiModuleV1 : NancyModule {
      private const string kApiVersion = "v1";
      private const string kApiBasePath = "api/" + kApiVersion;

      protected WebApiModuleV1(string pathOffset = null) : base(BuildPath(kApiBasePath, pathOffset)) { }

      public ClientDescriptor ClientDescriptor => GetClientDescriptor();

      public void Initialize() {
         SetupRoutes();
      }

      protected abstract void SetupRoutes();

      public Task<Response> BindAndValidate<TModel>(Func<TModel, Task<Response>> func) {
         var request = this.BindAndValidate<TModel>();
         if (!ModelValidationResult.IsValid) {
            return Task.FromResult(ValidationFailure());
         } else {
            return func(request);
         }
      }

      public Response Success(object result) {
         return Success(result, ResponseCode.Okay);
      }

      public Response Success(object result, ResponseCode code) {
         return Response.AsJson(new ResultResponse { ResponseCode = code, Result = result });
      }

      public Response Failure(string message) {
         return Failure(message, ResponseCode.Failure);
      }

      public Response Failure(string message, ResponseCode code) {
         return Response.AsJson(new ErrorResponse { ResponseCode = code, Error = message });
      }

      public Response Failure(string message, ResponseCode code, HttpStatusCode httpStatusCode) {
         return Response.AsJson(new ErrorResponse { ResponseCode = code, Error = message }, httpStatusCode);
      }

      public Response BadRequest(string message, ResponseCode code = ResponseCode.Failure) {
         return Response.AsJson(new ErrorResponse { ResponseCode = code, Error = message }, HttpStatusCode.BadRequest);
      }

      public Response Unauthorized(string message, ResponseCode code = ResponseCode.Unauthorized) {
         return Response.AsJson(new ErrorResponse { ResponseCode = code, Error = message }, HttpStatusCode.Unauthorized);
      }

      public Response InvalidToken() {
         return Response.AsJson(new ErrorResponse { ResponseCode = ResponseCode.ValidationError, Error = "Invalid access token." }, HttpStatusCode.Unauthorized);
      }

      public Response ValidationFailure() {
         return Response.AsJson(new ErrorResponse { ResponseCode = ResponseCode.ValidationError, Error = ModelValidationResult.Errors }, HttpStatusCode.BadRequest);
      }

      protected Func<object, CancellationToken, Task<dynamic>> Proxy<TResponse>(Func<object, CancellationToken, Task<TResponse>> func) {
         return async (parameters, token) => {
            var result = await func(parameters, token);
            return ConvertResponse(result);
         };
      }

      protected Func<object, CancellationToken, Task<dynamic>> Proxy<TParameter>(Func<object, CancellationToken, TParameter, Task<Response>> func) {
         return async (parameters, token) => {
            return await BindAndValidate<TParameter>(async x => {
               return ConvertResponse(await func(parameters, token, x));
            });
         };
      }

      public bool TryGetAccessToken(out string accessToken) {
         var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();
         if (authorizationHeader == null || !authorizationHeader.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase)) {
            accessToken = null;
            return false;
         } else {
            accessToken = authorizationHeader.Substring(authorizationHeader.IndexOf(' ') + 1);
            return true;
         }
      }

      private ClientDescriptor GetClientDescriptor() {
         var clientIdString = Request.Headers["X-Dargon-Client-Id"].FirstOrDefault();
         var clientFullName = Request.Headers["X-Dargon-Client-Full-Name"].FirstOrDefault();

         Guid clientId;
         if (!Guid.TryParse(clientIdString, out clientId)) {
            clientId = Guid.Empty;
         }

         string clientName = null, clientVersion = null;
         if (!string.IsNullOrWhiteSpace(clientFullName)) {
            var clientFullNameFirstSpaceIndex = clientFullName.IndexOf(' ');
            if (clientFullNameFirstSpaceIndex < 0) {
               clientName = clientFullName;
               clientVersion = "-";
            } else {
               clientName = clientFullName.Substring(0, clientFullNameFirstSpaceIndex).Trim();
               clientVersion = clientFullName.Substring(clientFullNameFirstSpaceIndex + 1).Trim();
            }
         }

         return new ClientDescriptor {
            Id = clientId,
            Name = clientName,
            Version = clientVersion
         };
      }

      private Response ConvertResponse<TResponse>(TResponse result) {
         var resultAsResponse = result as Response;
         if (resultAsResponse != null) {
            return resultAsResponse;
         } else if (result == null) {
            return new TextResponse("null", "application/json");
         } else {
            return Response.AsJson(result);
         }
      }

      private static string BuildPath(string apiBasePath, string pathOffset) {
         if (string.IsNullOrWhiteSpace(pathOffset)) {
            return apiBasePath;
         } else {
            return apiBasePath + "/" + pathOffset;
         }
      }
   }
}