using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer
{
    public sealed class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order { get; } = int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<HttpResponseExceptionFilter>>();
                logger.LogError(context.Exception, context.Exception.Message);

                if (context.Exception is ApiException apiException)
                {
                    var response =
                        new ErrorResponse
                        {
                            Code = apiException.Code,
                            Message = apiException.Message
                        };

                    context.Result = new ObjectResult(response) { StatusCode = apiException is Api400Exception ? 400 : 500 };
                }
                else
                {
                    var response =
                        new ErrorResponse
                        {
                            Code = Api500Exception.Codes.SERVER,
                            Message = context.Exception.Message
                        };

                    context.Result = new ObjectResult(response) { StatusCode = 500 };
                }

                context.ExceptionHandled = true;
            }
        }
    }
}
