using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TPLink.Gateway.Filters
{
    /// <summary>
    /// Translates any unhandled exception into the JSON error envelope used by the
    /// reference bridge: <c>{ "status": 500, "exception": { "name": ..., "message": ... } }</c>.
    /// </summary>
    public sealed class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            context.Result = new ObjectResult(new
            {
                status = 500,
                exception = new
                {
                    name = context.Exception.GetType().Name,
                    message = context.Exception.Message
                }
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }
    }
}
