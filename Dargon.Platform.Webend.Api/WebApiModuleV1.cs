using Nancy;
using Nancy.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;
using Nancy.ModelBinding;

namespace Dargon.Platform.Webend {
   public abstract class WebApiModuleV1 : NancyModule {
      private const string kApiVersion = "v1";
      private const string kApiBasePath = "api/" + kApiVersion;

      protected WebApiModuleV1(string pathOffset = null) : base(BuildPath(kApiBasePath, pathOffset)) { }

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
         return Success(ResponseCode.Okay, result);
      }

      public Response Success(ResponseCode code, object result) {
         return Response.AsJson(new ResultResponse { ResponseCode = code, Result = result });
      }

      public Response Failure(string message) {
         return Response.AsJson(new ErrorResponse { ResponseCode = ResponseCode.Failure, Error = message});
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